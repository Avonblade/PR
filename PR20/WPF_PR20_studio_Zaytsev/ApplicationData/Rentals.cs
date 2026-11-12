namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed partial class StudioRepository
{
    public Task<List<Rental>> RentalsAsync(int bookingId)
    {
        var session = Require();
        return Read(async db =>
        {
            var booking = await GetBooking(db,bookingId);
            Own(session,booking);
            return await db.ReadAsync($"""
                SELECT {PropColumns},COALESCE(x.Quantity,0),COALESCE(x.Rate,p.Rate),f.Free,
                    COALESCE((SELECT SUM(Quantity) FROM dbo.Loans WHERE BookingId=@id AND PropId=p.Id),0)
                FROM dbo.Props p CROSS APPLY dbo.PropFree(p.Id,@start,@end,@id) f
                LEFT JOIN dbo.Reservations x ON x.PropId=p.Id AND x.BookingId=@id
                WHERE @all=1 OR x.Quantity IS NOT NULL ORDER BY p.Name;
                """,r => new Rental(ReadProp(r),r.GetInt32(10),r.GetDecimal(11),r.GetInt32(12),r.GetInt32(13)),
                ("@id",bookingId),("@start",booking.Start),("@end",booking.End),("@all",session.Office));
        });
    }

    private static async Task EnsureReservations(StudioDatabase db,int id,DateTime start,DateTime end)
    {
        var errors = await db.ReadAsync("""
            SELECT p.Name,f.Free FROM dbo.Reservations x JOIN dbo.Props p ON p.Id=x.PropId
            CROSS APPLY dbo.PropFree(p.Id,@start,@end,@id) f WHERE x.BookingId=@id AND x.Quantity>f.Free;
            """,r => $"{r.GetString(0)} (свободно {r.GetInt32(1)})",("@id",id),("@start",start),("@end",end));
        if (errors.Count != 0)
            throw new StudioRuleException("Недостаточно реквизита на выбранное время: " + string.Join(", ",errors) + ".");
    }

    public Task ReserveAsync(int bookingId,int propId,int quantity)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        if (quantity is < 0 or > 1_000_000) throw new StudioRuleException("Укажите целое количество от нуля.");
        return Write(async db =>
        {
            var booking = await GetBooking(db,bookingId);
            if (booking.State is not (BookingState.Draft or BookingState.Confirmed or BookingState.Paid))
                throw new StudioRuleException("Подбор доступен до начала съёмки.");
            var prop = (await db.ReadAsync($"SELECT {PropColumns} FROM dbo.Props p WHERE p.Id=@id;",
                ReadProp,("@id",propId))).SingleOrDefault() ?? throw Stale();
            int free = await db.ScalarAsync<int>("SELECT Free FROM dbo.PropFree(@prop,@start,@end,@id);",
                ("@prop",propId),("@start",booking.Start),("@end",booking.End),("@id",booking.Id));
            if (quantity > free)
                throw new StudioRuleException($"На это время свободно {free} шт. Проверьте остаток и ремонт.");
            int issued = await db.ScalarAsync<int>("SELECT COALESCE(SUM(Quantity),0) FROM dbo.Loans WHERE BookingId=@id AND PropId=@prop;",
                ("@id",bookingId),("@prop",propId));
            if (quantity < issued)
                throw new StudioRuleException("Количество не может быть меньше уже выданного.");
            var reservation = (await db.ReadAsync("""
                SELECT Quantity,Rate FROM dbo.Reservations WHERE BookingId=@id AND PropId=@prop;
                """,r => (Quantity:r.GetInt32(0),Rate:r.GetDecimal(1)),("@id",bookingId),("@prop",propId))).SingleOrDefault();
            decimal appliedRate = reservation.Quantity == 0 ? prop.Rate : reservation.Rate;
            if (booking.Total + (quantity - reservation.Quantity) * appliedRate > MaxBookingTotal)
                throw new StudioRuleException("Итоговая стоимость превышает предел бронирования.");
            await db.ExecuteAsync("""
                IF @quantity=0 DELETE dbo.Reservations WHERE BookingId=@id AND PropId=@prop;
                ELSE IF EXISTS(SELECT 1 FROM dbo.Reservations WHERE BookingId=@id AND PropId=@prop)
                    UPDATE dbo.Reservations SET Quantity=@quantity WHERE BookingId=@id AND PropId=@prop;
                ELSE INSERT dbo.Reservations(BookingId,PropId,Quantity,Rate) VALUES(@id,@prop,@quantity,@rate);
                """,("@id",bookingId),("@prop",propId),("@quantity",quantity),("@rate",prop.Rate));
            await UpdatePriceStatus(db,booking);
        });
    }

    private static async Task UpdatePriceStatus(StudioDatabase db,Booking booking)
    {
        decimal total = await db.ScalarAsync<decimal>("SELECT Total FROM dbo.BookingTotals WHERE Id=@id;",("@id",booking.Id));
        if (booking.Paid > total) throw new StudioRuleException("Новая стоимость ниже принятой оплаты.");
        BookingState state = booking.State is BookingState.Paid or BookingState.Confirmed
            ? booking.Paid == total ? BookingState.Paid : BookingState.Confirmed
            : booking.State;
        await db.ExecuteAsync("UPDATE dbo.Bookings SET Status=@state WHERE Id=@id;",("@id",booking.Id),("@state",state));
    }

    public Task<List<Loan>> LoansAsync(int bookingId)
    {
        var session = Require();
        return Read(async db =>
        {
            Own(session,await GetBooking(db,bookingId));
            return await db.ReadAsync("""
                SELECT l.Id,p.Name,l.Quantity,l.IssuedAt,l.ReturnedAt,l.BeforeImageId,l.AfterImageId,l.DamagedCount,l.IssueNote,l.ReturnNote
                FROM dbo.Loans l JOIN dbo.Props p ON p.Id=l.PropId WHERE l.BookingId=@id ORDER BY l.Id DESC;
                """,r => new Loan(r.GetInt32(0),r.GetString(1),r.GetInt32(2),r.GetDateTime(3),Date(r,4),
                    r.GetInt32(5),Int(r,6),Int(r,7),Text(r,8),Text(r,9)),("@id",bookingId));
        });
    }

    public Task IssueAsync(int bookingId,int propId,int quantity,PhotoFile photo,string? note)
    {
        Require(StaffRole.Admin,StaffRole.Manager,StaffRole.Store);
        note = Optional(note,400,"Комментарий при выдаче");
        return Write(async db =>
        {
            var booking = await GetBooking(db,bookingId);
            if (booking.State is not (BookingState.Confirmed or BookingState.Paid or BookingState.InWork))
                throw new StudioRuleException("Выдача доступна для подтверждённой съёмки.");
            var prop = (await db.ReadAsync($"SELECT {PropColumns} FROM dbo.Props p WHERE p.Id=@id;",
                ReadProp,("@id",propId))).SingleOrDefault() ?? throw Stale();
            if (prop.Repair) throw new StudioRuleException("Реквизит находится на ремонте.");
            int reserved = await db.ScalarAsync<int>("""
                SELECT Quantity-COALESCE((SELECT SUM(Quantity) FROM dbo.Loans WHERE BookingId=@id AND PropId=@prop),0)
                FROM dbo.Reservations WHERE BookingId=@id AND PropId=@prop;
                """,("@id",bookingId),("@prop",propId));
            if (quantity <= 0 || quantity > Math.Min(prop.OnHand,reserved))
                throw new StudioRuleException($"Можно выдать не более {Math.Max(0,Math.Min(prop.OnHand,reserved))} шт. по этой брони.");
            int image = await SaveImage(db,photo);
            await db.ExecuteAsync("""
                INSERT dbo.Loans(BookingId,PropId,Quantity,IssuedAt,BeforeImageId,IssueNote)
                VALUES(@id,@prop,@qty,@now,@image,@note);
                """,("@id",bookingId),("@prop",propId),("@qty",quantity),("@now",Now()),("@image",image),("@note",note));
        });
    }

    public Task ReturnAsync(int bookingId,int loanId,int damaged,PhotoFile photo,string? note)
    {
        var session = Require();
        note = damaged > 0 ? Required(note,400,"Описание повреждений") : Optional(note,400,"Комментарий возврата");
        return Write(async db =>
        {
            var booking = await GetBooking(db,bookingId);
            Own(session,booking);
            var rows = await db.ReadAsync("""
                SELECT PropId,Quantity,ReturnedAt FROM dbo.Loans WHERE Id=@loan AND BookingId=@id;
                """,r => (Prop:r.GetInt32(0),Quantity:r.GetInt32(1),Returned:Date(r,2)),("@loan",loanId),("@id",bookingId));
            if (rows.Count == 0 || rows[0].Returned is not null)
                throw new StudioRuleException("Выдача уже закрыта или не найдена.");
            var loan = rows[0];
            if (damaged < 0 || damaged > loan.Quantity)
                throw new StudioRuleException("Количество повреждённых единиц не может превышать количество выданных.");
            int image = await SaveImage(db,photo);
            await db.ExecuteAsync("""
                UPDATE dbo.Loans SET ReturnedAt=@now,AfterImageId=@image,DamagedCount=@damaged,ReturnNote=@note WHERE Id=@loan;
                UPDATE dbo.Props SET Damaged=Damaged+@damaged WHERE Id=@prop;
                """,("@now",Now()),("@image",image),("@damaged",damaged),("@note",note),("@loan",loanId),("@prop",loan.Prop));
            if (damaged > 0) await SetRepairPeriod(db,loan.Prop,true);
        });
    }
}
