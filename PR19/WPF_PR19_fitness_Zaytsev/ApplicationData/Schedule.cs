using Microsoft.Data.SqlClient;

namespace WPF_PR19_fitness_Zaytsev.ApplicationData;

internal sealed partial class FitnessRepository
{
    private const string TrainingColumns = """
        t.Id, t.Name, t.RoomId, r.Name, t.Type, t.CoachId, e.FullName, t.MaxParticipants, r.Capacity,
        t.Weekday, t.StartsAt, t.EndsAt, t.Version
        """;

    public Task<List<ScheduleRow>> ScheduleAsync(DateTime week)
    {
        Require();
        week = week.Date.AddDays(1 - FitnessText.WeekdayOf(week));
        return ReadAsync(db => db.ReadAsync($"""
            SELECT {TrainingColumns}, dates.SessionOn,
                (SELECT COUNT(*) FROM dbo.Bookings b WHERE b.TrainingId = t.Id AND b.SessionOn = dates.SessionOn)
            FROM dbo.Trainings t JOIN dbo.Rooms r ON r.Id = t.RoomId JOIN dbo.Employees e ON e.Id = t.CoachId
            CROSS APPLY (SELECT DATEADD(DAY, t.Weekday - 1, @week) AS SessionOn) dates
            ORDER BY t.Weekday, t.StartsAt, t.Name;
            """, reader => new ScheduleRow(ReadTraining(reader), reader.GetDateTime(13), reader.GetInt32(14)), ("@week", week)));
    }

    public Task BookAsync(int trainingId, DateTime day, int clientId, bool guest)
    {
        Require(StaffRole.Admin, StaffRole.Sales);
        return WriteAsync(async db =>
        {
            var training = await GetTrainingAsync(db, trainingId);
            ValidateSession(training, day);
            if (day.Date + training.EndsAt <= Now())
                throw new FitnessRuleException("Запись на завершившуюся тренировку закрыта.");

            var client = await GetClientAsync(db, clientId);
            if (client.Status != ClientStatus.Active)
                throw new FitnessRuleException("На тренировку можно записать только активного клиента.");

            if (await db.ScalarAsync<int>("""
                SELECT TOP (1) 1 FROM dbo.Bookings WHERE TrainingId = @training AND SessionOn = @day AND ClientId = @client;
                """, ("@training", trainingId), ("@day", day.Date), ("@client", clientId)) == 1)
                throw new FitnessRuleException("Клиент уже записан на эту тренировку.");

            int booked = await db.ScalarAsync<int>("""
                SELECT COUNT(*) FROM dbo.Bookings WHERE TrainingId = @training AND SessionOn = @day;
                """, ("@training", trainingId), ("@day", day.Date));
            if (booked >= training.Capacity)
                throw new FitnessRuleException("Свободных мест нет. Достигнута вместимость зала или тренировки.");

            int? membershipId = null;
            if (!guest)
            {
                var plan = await ActivePlanAsync(db, clientId, day);
                if (plan is null || plan.Remaining == 0)
                    throw new FitnessRuleException("На дату занятия нужен активный абонемент с доступным посещением. Можно выбрать гостевую запись.");
                membershipId = plan.Id;
            }

            await db.ExecuteAsync("""
                INSERT dbo.Bookings (TrainingId, ClientId, SessionOn, MembershipId, IsGuest, Attendance)
                VALUES (@training, @client, @day, @plan, @guest, N'Pending');
                """, ("@training", trainingId), ("@client", clientId), ("@day", day.Date),
                ("@plan", membershipId), ("@guest", guest));
        });
    }

    public Task<List<BookingRow>> BookingsAsync(int trainingId, DateTime day)
    {
        var session = Require(StaffRole.Admin, StaffRole.Coach);
        return ReadAsync(async db =>
        {
            var training = await GetTrainingAsync(db, trainingId);
            EnsureCoach(session, training);
            ValidateSession(training, day);
            return await db.ReadAsync("""
                SELECT b.Id, c.FullName, b.IsGuest, b.Attendance
                FROM dbo.Bookings b JOIN dbo.Clients c ON c.Id = b.ClientId
                WHERE b.TrainingId = @training AND b.SessionOn = @day
                ORDER BY c.FullName;
                """, reader => new BookingRow(reader.GetInt32(0), reader.GetString(1), reader.GetBoolean(2),
                    Code<Attendance>(reader, 3)), ("@training", trainingId), ("@day", day.Date));
        });
    }

    public Task MarkAsync(int bookingId, Attendance state)
    {
        var session = Require(StaffRole.Admin, StaffRole.Coach);
        if (state is not (Attendance.Present or Attendance.Absent))
            throw new FitnessRuleException("Выберите отметку присутствия.");

        return WriteAsync(async db =>
        {
            var bookings = await db.ReadAsync("""
                SELECT TrainingId, ClientId, SessionOn, MembershipId, IsGuest, Attendance
                FROM dbo.Bookings WHERE Id = @id;
                """, reader => (Training: reader.GetInt32(0), Client: reader.GetInt32(1), Day: reader.GetDateTime(2),
                    Plan: Number(reader, 3), Guest: reader.GetBoolean(4), State: Code<Attendance>(reader, 5)), ("@id", bookingId));
            if (bookings.Count == 0) throw Stale();
            var booking = bookings[0];
            var training = await GetTrainingAsync(db, booking.Training);
            EnsureCoach(session, training);
            if (booking.State != Attendance.Pending)
                throw new FitnessRuleException("Для участника уже установлена отметка.");

            DateTime entered = booking.Day + training.StartsAt;
            DateTime ends = booking.Day + training.EndsAt;
            DateTime now = Now();
            if (entered > now)
                throw new FitnessRuleException("Отметка доступна после начала занятия.");

            if (state == Attendance.Present)
            {
                var client = await GetClientAsync(db, booking.Client);
                if (client.Status != ClientStatus.Active)
                    throw new FitnessRuleException("Клиент больше не активен. Посещение не зарегистрировано.");
                if (!booking.Guest)
                {
                    var plan = await GetPlanAsync(db, booking.Plan!.Value);
                    if (plan.StateOn(booking.Day) != PlanState.Active || plan.Remaining == 0)
                        throw new FitnessRuleException("Абонемент недоступен или лимит посещений исчерпан.");
                }

                DateTime? exited = ends <= now ? ends : null;
                await db.ExecuteAsync("""
                    INSERT dbo.Visits (ClientId, MembershipId, EnteredAt, ExitedAt, AccessType, CoachId, BookingId)
                    VALUES (@client, @plan, @entered, @exited, N'Group', @coach, @booking);
                    """, ("@client", booking.Client), ("@plan", booking.Plan), ("@entered", entered),
                    ("@exited", exited), ("@coach", training.CoachId), ("@booking", bookingId));
            }
            await db.ExecuteAsync("UPDATE dbo.Bookings SET Attendance = @state WHERE Id = @id;",
                ("@state", state), ("@id", bookingId));
        });
    }

    private static void EnsureCoach(StaffSession session, Training training)
    {
        if (!session.Admin && training.CoachId != session.Id)
            throw new FitnessRuleException("Список участников доступен только тренеру этого занятия.");
    }

    private static void ValidateSession(Training training, DateTime day)
    {
        if (FitnessText.WeekdayOf(day) != training.Weekday)
            throw new FitnessRuleException("Дата не соответствует дню недели тренировки.");
    }

    private static async Task<Training> GetTrainingAsync(FitnessDatabase db, int id)
        => (await db.ReadAsync($"""
            SELECT {TrainingColumns} FROM dbo.Trainings t
            JOIN dbo.Rooms r ON r.Id = t.RoomId JOIN dbo.Employees e ON e.Id = t.CoachId WHERE t.Id = @id;
            """, ReadTraining, ("@id", id))).SingleOrDefault() ?? throw Stale();

    private static Training ReadTraining(SqlDataReader reader)
        => new(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetString(3),
            Code<WorkoutType>(reader, 4), reader.GetInt32(5), reader.GetString(6), reader.GetInt32(7),
            reader.GetInt32(8), reader.GetInt32(9), reader.GetTimeSpan(10), reader.GetTimeSpan(11), reader.GetFieldValue<byte[]>(12));
}
