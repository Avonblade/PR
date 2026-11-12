using System.IO;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed partial class StudioRepository
{
    public Task<List<Material>> MaterialsAsync(int id)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Photo);
        return Read(async db =>
        {
            Own(session,await GetBooking(db,id));
            return await db.ReadAsync("""
                SELECT m.Id,m.Kind,m.Status,m.ImageId,COALESCE(i.Filename,m.Location),m.Comment,m.UploadedAt,m.ReadyAt
                FROM dbo.Materials m LEFT JOIN dbo.Images i ON i.Id=m.ImageId WHERE BookingId=@id ORDER BY m.Id DESC;
                """,r => new Material(r.GetInt32(0),Code<MaterialKind>(r,1),Code<MaterialState>(r,2),Int(r,3),
                    r.GetString(4),Text(r,5),r.GetDateTime(6),Date(r,7)),("@id",id));
        });
    }

    public Task AddMaterialAsync(int id,MaterialKind kind,MaterialState status,PhotoFile? image,string? location,string? note)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Photo);
        if (!Enum.IsDefined(kind) || !Enum.IsDefined(status)) throw new StudioRuleException("Выберите тип и статус материала.");
        if (session.Role == StaffRole.Manager && kind != MaterialKind.Preview)
            throw new StudioRuleException("Менеджер может загружать только превью.");
        note = Optional(note,500,"Комментарий");
        if (kind == MaterialKind.Preview)
        {
            if (image is null) throw new StudioRuleException("Выберите фотографию превью.");
            location = null;
        }
        else
        {
            location = Required(location,1000,"Путь или ссылка");
            bool webLink = Uri.TryCreate(location,UriKind.Absolute,out var uri) && (uri.Scheme is "http" or "https") && uri.Host.Length > 0;
            if (!webLink && !Path.IsPathFullyQualified(location))
                throw new StudioRuleException("Укажите абсолютный путь к файлу или ссылку HTTP(S).");
            image = null;
        }

        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            Own(session,booking);
            if (booking.State is BookingState.Draft or BookingState.Cancelled)
                throw new StudioRuleException("Загрузка доступна для подтверждённой съёмки.");
            int? imageId = null;
            if (image is not null)
            {
                if (await PreviewCount(db,id) >= 5) throw new StudioRuleException("Для съёмки можно загрузить до пяти превью.");
                imageId = await SaveImage(db,image);
                if (await db.ScalarAsync<int>("SELECT TOP(1) 1 FROM dbo.Materials WHERE BookingId=@id AND ImageId=@image;",
                    ("@id",id),("@image",imageId)) != 0)
                    throw new StudioRuleException("Это превью уже загружено.");
            }
            DateTime now = Now();
            await db.ExecuteAsync("""
                INSERT dbo.Materials(BookingId,Kind,Status,ImageId,Location,Comment,UploadedAt,ReadyAt)
                VALUES(@id,@kind,@status,@image,@location,@note,@now,@ready);
                """,("@id",id),("@kind",kind),("@status",status),("@image",imageId),
                ("@location",location),("@note",note),("@now",now),("@ready",status == MaterialState.Ready ? now : null));
        });
    }

    public Task UpdateMaterialAsync(int id,Material item,MaterialState state,string? note)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Photo);
        if (!Enum.IsDefined(state)) throw new StudioRuleException("Выберите статус материала.");
        note = Optional(note,500,"Комментарий");
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            Own(session,booking);
            if (booking.State == BookingState.Cancelled) throw new StudioRuleException("Съёмка отменена.");
            if (await db.ExecuteAsync("""
                UPDATE dbo.Materials SET Status=@state,Comment=@note,
                    ReadyAt=CASE WHEN @state=N'Ready' THEN COALESCE(ReadyAt,@now) ELSE NULL END
                WHERE BookingId=@id AND Id=@material AND (@preview=0 OR Kind=N'Preview');
                """,("@state",state),("@note",note),("@now",Now()),("@id",id),("@material",item.Id),
                ("@preview",session.Role == StaffRole.Manager)) == 0)
                throw new StudioRuleException("Материал не найден или недоступен для редактирования.");
        });
    }

    public Task SaveNotesAsync(int id,string? note)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Photo);
        note = Optional(note,1000,"Комментарий к съёмке");
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            Own(session,booking);
            if (booking.State == BookingState.Cancelled || booking.State == BookingState.Completed && !session.Admin)
                throw new StudioRuleException("Редактирование комментария для этого статуса недоступно.");
            await db.ExecuteAsync("UPDATE dbo.Bookings SET Notes=@note WHERE Id=@id;",("@id",id),("@note",note));
        });
    }

    public Task StartAsync(int id)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Photo);
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            Own(session,booking);
            if (booking.State is not (BookingState.Confirmed or BookingState.Paid))
                throw new StudioRuleException("Начать можно только подтверждённую или оплаченную съёмку.");
            await db.ExecuteAsync("UPDATE dbo.Bookings SET Status=N'InWork' WHERE Id=@id;",("@id",id));
        });
    }

    public Task RequestFinishAsync(int id)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Photo);
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            Own(session,booking);
            await EnsureFinish(db,booking);
            await db.ExecuteAsync("""
                UPDATE dbo.Bookings SET FinishRequestedAt=COALESCE(FinishRequestedAt,@now) WHERE Id=@id;
                """,("@id",id),("@now",Now()));
        });
    }

    public Task CloseAsync(int id,string? agreement,DateTime? due,bool qualityIssue)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        agreement = Optional(agreement,250,"Согласование с клиентом");
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            await EnsureFinish(db,booking);
            if (booking.Balance > 0 && (agreement is null || due is null))
                throw new StudioRuleException("Погасите остаток или укажите согласование с клиентом и дату оплаты.");
            if (booking.Balance > 0 && due?.Year is < 2000 or > 2099)
                throw new StudioRuleException("Укажите срок оплаты в диапазоне 2000–2099 годов.");
            await db.ExecuteAsync("""
                UPDATE dbo.Bookings SET Status=N'Completed',CompletedAt=@now,DebtNote=@note,DueOn=@due,QualityIssue=@quality
                WHERE Id=@id;
                """,("@id",id),("@now",Now()),("@note",booking.Balance > 0 ? agreement : null),
                ("@due",booking.Balance > 0 ? due?.Date : null),("@quality",qualityIssue));
        });
    }

    public Task UpdateQualityAsync(int id,bool issue)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        return Write(async db =>
        {
            if (await db.ExecuteAsync("UPDATE dbo.Bookings SET QualityIssue=@issue WHERE Id=@id AND Status=N'Completed';",
                ("@issue",issue),("@id",id)) != 1)
                throw new StudioRuleException("Отметка доступна для завершённой съёмки.");
        });
    }

    private static Task<int> PreviewCount(StudioDatabase db,int id) =>
        db.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Materials WHERE BookingId=@id AND Kind=N'Preview';",("@id",id));

    private static async Task EnsureFinish(StudioDatabase db,Booking booking)
    {
        if (booking.State != BookingState.InWork)
            throw new StudioRuleException("Съёмка должна быть в работе.");
        if (await PreviewCount(db,booking.Id) < 3)
            throw new StudioRuleException("Загрузите минимум три разных превью-фото.");
        if (await OpenLoans(db,booking.Id) > 0)
            throw new StudioRuleException("Примите весь выданный реквизит и загрузите фотографии состояния при возврате.");
    }

    public Task<List<Payment>> PaymentsAsync(int id)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        return Read(db => db.ReadAsync("""
            SELECT Id,Amount,Method,Receipt,PaidAt,IsAdvance FROM dbo.Payments WHERE BookingId=@id ORDER BY Id DESC;
            """,r => new Payment(r.GetInt32(0),r.GetDecimal(1),Code<PaymentMethod>(r,2),
                r.GetString(3),r.GetDateTime(4),r.GetBoolean(5)),("@id",id)));
    }

    public Task<int> PayAsync(int id,decimal amount,PaymentMethod method,string receipt)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        receipt = Required(receipt,60,"Номер чека");
        if (amount is <= 0 or > 9_999_999_999.99m || decimal.Round(amount,2) != amount || !Enum.IsDefined(method))
            throw new StudioRuleException("Введите положительную сумму с точностью до копеек и способ оплаты.");
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            if (booking.State is BookingState.Draft or BookingState.Cancelled)
                throw new StudioRuleException("Оплата доступна после подтверждения брони.");
            if (amount > booking.Balance)
                throw new StudioRuleException($"Сумма оплаты не может превышать остаток {StudioText.Money(booking.Balance)}.");
            int paymentId = await db.ScalarAsync<int>("""
                INSERT dbo.Payments(BookingId,Amount,Method,Receipt,PaidAt,IsAdvance)
                OUTPUT INSERTED.Id VALUES(@id,@amount,@method,@receipt,@now,@advance);
                """,("@id",id),("@amount",amount),("@method",method),("@receipt",receipt),("@now",Now()),
                ("@advance",booking.State == BookingState.Confirmed));
            var state = booking.State == BookingState.Confirmed && amount == booking.Balance ? BookingState.Paid : booking.State;
            await db.ExecuteAsync("UPDATE dbo.Bookings SET Status=@state WHERE Id=@id;",("@state",state),("@id",id));
            return paymentId;
        });
    }
}
