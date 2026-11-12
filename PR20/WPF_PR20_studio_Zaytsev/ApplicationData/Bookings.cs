using Microsoft.Data.SqlClient;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed partial class StudioRepository
{
    private const string BookingSelect = """
        SELECT b.Id,b.ClientId,c.FullName,b.RoomId,r.Name,b.PhotographerId,s.FullName,b.StartsAt,b.EndsAt,
            b.ShootType,b.Status,b.HourlyRate,t.Total,t.Paid,t.Advance,b.Notes,b.FinishRequestedAt,b.QualityIssue,
            CONVERT(bit,CASE WHEN b.Status NOT IN (N'Completed',N'Cancelled') AND EXISTS (
                SELECT 1 FROM dbo.Bookings x WHERE x.RoomId=b.RoomId AND x.Id<>b.Id
                AND x.Status IN (N'Confirmed',N'Paid',N'InWork') AND x.StartsAt<b.EndsAt AND x.EndsAt>b.StartsAt
            ) THEN 1 ELSE 0 END),
            CONVERT(bit,CASE WHEN NOT EXISTS(SELECT 1 FROM dbo.Reservations x WHERE x.BookingId=b.Id) THEN NULL
                WHEN EXISTS(SELECT 1 FROM dbo.Reservations x
                    CROSS APPLY dbo.PropFree(x.PropId,b.StartsAt,b.EndsAt,b.Id) f
                    WHERE x.BookingId=b.Id AND x.Quantity>f.Free) THEN 0 ELSE 1 END),
            b.Version
        FROM dbo.Bookings b JOIN dbo.Clients c ON c.Id=b.ClientId JOIN dbo.Rooms r ON r.Id=b.RoomId
        JOIN dbo.Staff s ON s.Id=b.PhotographerId JOIN dbo.BookingTotals t ON t.Id=b.Id
        """;

    public Task<List<Booking>> BookingsAsync(int? clientId = null)
    {
        var session = Require();
        return Read(db => db.ReadAsync(BookingSelect + " " + """
            WHERE (@photo=0 OR b.PhotographerId=@photo) AND (@client=0 OR b.ClientId=@client)
            ORDER BY b.StartsAt,b.Id;
            """, ReadBooking, ("@photo",session.Photographer ? session.Id : 0),("@client",clientId ?? 0)));
    }

    private static async Task<Booking> GetBooking(StudioDatabase db,int id)
        => (await db.ReadAsync(BookingSelect + " WHERE b.Id=@id;",ReadBooking,("@id",id))).SingleOrDefault() ?? throw Stale();

    public Task<Booking> BookingAsync(int id)
    {
        var session = Require();
        return Read(async db =>
        {
            var booking = await GetBooking(db,id);
            Own(session,booking);
            return booking;
        });
    }

    private static Booking ReadBooking(SqlDataReader r) =>
        new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetInt32(3),r.GetString(4),r.GetInt32(5),r.GetString(6),
            r.GetDateTime(7),r.GetDateTime(8),r.GetString(9),Code<BookingState>(r,10),r.GetDecimal(11),r.GetDecimal(12),
            r.GetDecimal(13),r.GetDecimal(14),Text(r,15),Date(r,16),r.GetBoolean(17),r.GetBoolean(18),
            r.IsDBNull(19) ? null : r.GetBoolean(19),Version(r,20));

    public Task<SlotCheck> CheckSlotAsync(BookingInput input,int exclude = 0)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        ValidateTime(input);
        return Read(db => CheckSlot(db,input,exclude));
    }

    private static void ValidateTime(BookingInput input)
    {
        if (input.Start.Year is < 2000 or > 2099 || input.Start.TimeOfDay.Ticks % TimeSpan.FromMinutes(30).Ticks != 0)
            throw new StudioRuleException("Укажите начало с шагом 30 минут.");
        if (input.Minutes is < 60 or > 720 || input.Minutes % 30 != 0)
            throw new StudioRuleException("Длительность от 1 до 12 часов с шагом 0,5 часа.");
        if (!StudioText.Types.Contains(input.Type))
            throw new StudioRuleException("Выберите тип съёмки.");
    }

    private static async Task<SlotCheck> CheckSlot(StudioDatabase db,BookingInput input,int exclude)
    {
        var rooms = await ReadRooms(db);
        var staff = await ReadStaff(db);
        var activeRooms = rooms.Where(r => !r.Repair).ToList();
        var activePhotos = staff.Where(s => s.Active && s.Role == StaffRole.Photo).ToList();
        var busy = await db.ReadAsync("""
            SELECT b.Id,b.RoomId,r.Name,b.PhotographerId,s.FullName,b.StartsAt,b.EndsAt
            FROM dbo.Bookings b JOIN dbo.Rooms r ON r.Id=b.RoomId JOIN dbo.Staff s ON s.Id=b.PhotographerId
            WHERE b.Id<>@exclude AND b.Status IN (N'Confirmed',N'Paid',N'InWork')
              AND b.StartsAt<@limit AND b.EndsAt>@start ORDER BY b.StartsAt,b.Id;
            """,r => (Id:r.GetInt32(0),Room:r.GetInt32(1),RoomName:r.GetString(2),Photo:r.GetInt32(3),
                PhotoName:r.GetString(4),Start:r.GetDateTime(5),End:r.GetDateTime(6)),
            ("@exclude",exclude),("@limit",input.Start.AddDays(14)),("@start",input.Start));

        DateTime end = input.Start.AddMinutes(input.Minutes);
        var occupied = busy.Where(b => b.Start < end && b.End > input.Start).ToList();
        var conflicts = occupied.Where(b => b.Room == input.RoomId || b.Photo == input.PhotoId).Select(b =>
        {
            string resource = b.Room == input.RoomId && b.Photo == input.PhotoId ? "зал и фотограф" :
                b.Room == input.RoomId ? "зал" : "фотограф";
            return $"ФС-{b.Id:0000} · {StudioText.Stamp(b.Start)} – {StudioText.Stamp(b.End)} · {resource} ({b.RoomName}, {b.PhotoName})";
        }).ToList();
        var freePhotos = activePhotos.Where(p => occupied.All(b => b.Photo != p.Id)).Select(p => p.Id).ToHashSet();

        var alternatives = new List<Slot>();
        foreach (var room in activeRooms.OrderByDescending(r => r.Id == input.RoomId))
        {
            if (occupied.Any(b => b.Room == room.Id)) continue;
            foreach (var photo in activePhotos.Where(p => freePhotos.Contains(p.Id)).OrderByDescending(p => p.Id == input.PhotoId))
            {
                if (room.Id != input.RoomId || photo.Id != input.PhotoId)
                    alternatives.Add(new Slot(input.Start,room.Id,room.Name,photo.Id,photo.Name));
                if (alternatives.Count == 4) break;
            }
            if (alternatives.Count == 4) break;
        }

        var selectedRoom = activeRooms.Find(r => r.Id == input.RoomId);
        var selectedPhoto = activePhotos.Find(p => p.Id == input.PhotoId);
        if (selectedRoom is not null)
        {
            int later = 0;
            for (var start = input.Start.AddMinutes(30); start < input.Start.AddDays(13) && start.Year <= 2099 && later < 4;
                start = start.AddMinutes(30))
            {
                DateTime stop = start.AddMinutes(input.Minutes);
                var collisions = busy.Where(b => b.Start < stop && b.End > start).ToList();
                if (collisions.Any(b => b.Room == input.RoomId)) continue;
                var available = selectedPhoto is null
                    ? activePhotos.Find(p => collisions.All(b => b.Photo != p.Id))
                    : collisions.All(b => b.Photo != selectedPhoto.Id) ? selectedPhoto : null;
                if (available is null) continue;
                alternatives.Add(new Slot(start,selectedRoom.Id,selectedRoom.Name,available.Id,available.Name));
                later++;
            }
        }

        return new SlotCheck(conflicts,alternatives,freePhotos);
    }

    public async Task<int> SaveBookingAsync(BookingInput input,Booking? original,bool confirm)
    {
        var session = Require(StaffRole.Admin,StaffRole.Manager);
        ValidateTime(input);
        var result = await Write(async db =>
        {
            Booking? previous = original is null ? null : await GetBooking(db,original.Id);
            if (original is not null && !original.Version.SequenceEqual(previous!.Version)) throw Stale();
            if (previous?.State is BookingState.Cancelled || previous?.State is BookingState.InWork or BookingState.Completed && !session.Admin)
                throw new StudioRuleException("Эту съёмку нельзя переносить с текущими правами.");
            if (previous is not null && previous.Paid > 0 && previous.ClientId != input.ClientId)
                throw new StudioRuleException("После оплаты нельзя менять клиента в бронировании.");
            if (previous is not null && previous.State == BookingState.InWork &&
                (previous.RoomId != input.RoomId || previous.PhotoId != input.PhotoId || previous.Start != input.Start))
                throw new StudioRuleException("Для съёмки в работе можно исправить окончание и тип, сохранив зал, фотографа и начало.");

            var room = (await ReadRooms(db)).SingleOrDefault(r => r.Id == input.RoomId);
            var photo = (await ReadStaff(db)).SingleOrDefault(s => s.Id == input.PhotoId);
            bool historical = previous?.State == BookingState.Completed;
            if (room is null || room.Repair && !(historical && previous!.RoomId == room.Id))
                throw new StudioRuleException("Выберите активный зал.");
            if (photo is null || photo.Role != StaffRole.Photo || !photo.Active && !(historical && previous!.PhotoId == photo.Id))
                throw new StudioRuleException("Выберите действующего фотографа.");
            var client = (await ReadClients(db)).SingleOrDefault(c => c.Id == input.ClientId);
            if (client is null || client.Blocked && !(historical && previous!.ClientId == client.Id))
                throw new StudioRuleException("Клиент не найден или находится в чёрном списке.");

            DateTime end = input.Start.AddMinutes(input.Minutes);
            int id = previous?.Id ?? 0;
            bool reserve = !historical && (confirm || previous?.State is BookingState.Confirmed or BookingState.Paid or BookingState.InWork);
            if (reserve)
            {
                var check = await CheckSlot(db,input,id);
                if (check.Conflicts.Count > 0)
                {
                    await db.ExecuteAsync("""
                        INSERT dbo.Conflicts(RoomId,PhotographerId,RoomConflict,PhotoConflict,LoggedAt)
                        SELECT @room,@photo,CONVERT(bit,MAX(CASE WHEN RoomId=@room THEN 1 ELSE 0 END)),
                            CONVERT(bit,MAX(CASE WHEN PhotographerId=@photo THEN 1 ELSE 0 END)),@now
                        FROM dbo.Bookings WHERE Id<>@id AND StartsAt<@end AND EndsAt>@start
                            AND Status IN(N'Confirmed',N'Paid',N'InWork') AND (RoomId=@room OR PhotographerId=@photo);
                        """,("@room",input.RoomId),("@photo",input.PhotoId),("@now",Now()),("@id",id),("@end",end),("@start",input.Start));
                    return (Id:0,Error:Describe(check));
                }
                await EnsureReservations(db,id,input.Start,end);
            }

            decimal rate = previous?.RoomId == input.RoomId ? previous.HourlyRate : room.Rate;
            decimal props = await db.ScalarAsync<decimal>("""
                SELECT COALESCE(SUM(Rate*Quantity),0) FROM dbo.Reservations WHERE BookingId=@id;
                """,("@id",id));
            decimal total = Math.Round(rate * input.Minutes / 60 + props,2,MidpointRounding.AwayFromZero);
            decimal paid = previous?.Paid ?? 0;
            if (total > MaxBookingTotal)
                throw new StudioRuleException("Итоговая стоимость превышает предел бронирования.");
            if (paid > total)
                throw new StudioRuleException("Новая стоимость ниже принятой оплаты.");
            if (historical && total > paid && previous!.Total == previous.Paid)
                throw new StudioRuleException("Исправление завершённой съёмки не должно создавать несогласованный долг.");

            BookingState state = historical ? BookingState.Completed : previous?.State == BookingState.InWork ? BookingState.InWork :
                !reserve ? BookingState.Draft : paid == total ? BookingState.Paid : BookingState.Confirmed;
            string command = id == 0 ? """
                INSERT dbo.Bookings(ClientId,RoomId,PhotographerId,StartsAt,EndsAt,ShootType,Status,HourlyRate)
                OUTPUT INSERTED.Id VALUES(@client,@room,@photo,@start,@end,@type,@state,@rate);
                """ : """
                UPDATE dbo.Bookings SET ClientId=@client,RoomId=@room,PhotographerId=@photo,StartsAt=@start,EndsAt=@end,
                    ShootType=@type,Status=@state,HourlyRate=@rate OUTPUT INSERTED.Id WHERE Id=@id;
                """;
            int saved = await db.ScalarAsync<int>(command,("@client",input.ClientId),("@room",input.RoomId),("@photo",input.PhotoId),
                ("@start",input.Start),("@end",end),("@type",input.Type),("@state",state),("@rate",rate),("@id",id));
            return (Id:saved,Error:(string?)null);
        });
        if (result.Error is not null) throw new StudioRuleException(result.Error);
        return result.Id;
    }

    public static string Describe(SlotCheck check)
    {
        string conflicts = check.Conflicts.Count == 0 ? "Зал и фотограф свободны." :
            "Пересечения:\n" + string.Join("\n",check.Conflicts);
        string alternatives = check.Alternatives.Count == 0 ? "\nСвободных вариантов на ближайшие две недели не найдено." :
            "\nВарианты:\n" + string.Join("\n",check.Alternatives.Take(5).Select(s => s.Caption));
        return conflicts + alternatives;
    }

    public Task CancelAsync(int id,string reason)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        reason = Required(reason,250,"Причина отмены");
        return Write(async db =>
        {
            var booking = await GetBooking(db,id);
            if (booking.State is BookingState.Cancelled or BookingState.Completed)
                throw new StudioRuleException("Завершённую или отменённую бронь нельзя отменить.");
            if (await OpenLoans(db,id) > 0)
                throw new StudioRuleException("Сначала примите выданный реквизит.");
            await db.ExecuteAsync("""
                UPDATE dbo.Bookings SET Status=N'Cancelled',CancelledAt=@now,CancelReason=@reason WHERE Id=@id;
                """,("@id",id),("@now",Now()),("@reason",reason));
        });
    }

    private static Task<int> OpenLoans(StudioDatabase db,int id) =>
        db.ScalarAsync<int>("SELECT COUNT(*) FROM dbo.Loans WHERE BookingId=@id AND ReturnedAt IS NULL;",("@id",id));
}
