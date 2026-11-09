using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace WPF_PR19_fitness_Zaytsev.ApplicationData;

internal sealed partial class FitnessRepository
{
    public Task<List<Room>> RoomsAsync()
    {
        Require();
        return ReadAsync(db => db.ReadAsync("""
            SELECT Id, Name, Capacity, Version FROM dbo.Rooms ORDER BY Name;
            """, reader => new Room(reader.GetInt32(0), reader.GetString(1), reader.GetInt32(2), reader.GetFieldValue<byte[]>(3))));
    }

    public Task SaveRoomAsync(Room? original, string name, int capacity)
    {
        Require(StaffRole.Admin);
        name = Required(name, 60, "Зал");
        if (capacity <= 0) throw new FitnessRuleException("Вместимость должна быть больше нуля.");
        return WriteAsync(async db =>
        {
            if (original is null)
            {
                await db.ExecuteAsync("INSERT dbo.Rooms (Name, Capacity) VALUES (@name, @capacity);",
                    ("@name", name), ("@capacity", capacity));
                return;
            }
            if (await db.ScalarAsync<int>("SELECT COALESCE(MAX(MaxParticipants), 0) FROM dbo.Trainings WHERE RoomId = @id;",
                ("@id", original.Id)) > capacity)
                throw new FitnessRuleException("Вместимость не может быть меньше лимита назначенной тренировки.");
            if (await db.ExecuteAsync("""
                UPDATE dbo.Rooms SET Name = @name, Capacity = @capacity WHERE Id = @id AND Version = @version;
                """, ("@name", name), ("@capacity", capacity), ("@id", original.Id), ("@version", original.Version)) == 0)
                throw Stale();
        });
    }

    public Task<List<Employee>> EmployeesAsync()
    {
        Require(StaffRole.Admin);
        return ReadAsync(db => db.ReadAsync("""
            SELECT Id, FullName, Role, Login, Phone, IsActive, Version FROM dbo.Employees ORDER BY FullName;
            """, ReadEmployee));
    }

    public async Task SaveEmployeeAsync(Employee? original, string name, StaffRole role, string login,
        string phone, bool active, string password)
    {
        var session = Require(StaffRole.Admin);
        name = Required(name, 100, "ФИО");
        login = Required(login, 50, "Логин");
        phone = Required(phone, 25, "Телефон");
        if (!Enum.IsDefined(role))
            throw new FitnessRuleException("Выберите роль сотрудника.");
        if ((original is null || password.Length > 0) && password.Length is < 8 or > 128)
            throw new FitnessRuleException("Длина пароля должна составлять от 8 до 128 знаков.");
        if (original?.Id == session.Id && (!active || role != StaffRole.Admin))
            throw new FitnessRuleException("Нельзя отключить свою учётную запись или сменить её роль.");

        byte[]? salt = password.Length == 0 ? null : RandomNumberGenerator.GetBytes(16);
        byte[]? hash = salt is null ? null : await Task.Run(() => HashPassword(password, salt));
        try
        {
            await WriteAsync(async db =>
            {
                if (original is null)
                {
                    await db.ExecuteAsync("""
                        INSERT dbo.Employees (FullName, Role, Login, Phone, IsActive, PasswordSalt, PasswordHash)
                        VALUES (@name, @role, @login, @phone, @active, @salt, @hash);
                        """, ("@name", name), ("@role", role), ("@login", login), ("@phone", phone),
                        ("@active", active), ("@salt", salt), ("@hash", hash));
                    return;
                }
                if (original.Role == StaffRole.Coach && (!active || role != StaffRole.Coach) &&
                    await db.ScalarAsync<int>("SELECT TOP (1) 1 FROM dbo.Trainings WHERE CoachId = @id;", ("@id", original.Id)) == 1)
                    throw new FitnessRuleException("Сначала переназначьте тренировки сотрудника.");
                string passwordUpdate = salt is null ? "" : ", PasswordSalt = @salt, PasswordHash = @hash";
                if (await db.ExecuteAsync($"""
                    UPDATE dbo.Employees SET FullName = @name, Role = @role, Login = @login, Phone = @phone,
                        IsActive = @active{passwordUpdate} WHERE Id = @id AND Version = @version;
                    """, ("@name", name), ("@role", role), ("@login", login), ("@phone", phone),
                    ("@active", active), ("@salt", salt), ("@hash", hash), ("@id", original.Id),
                    ("@version", original.Version)) == 0)
                    throw Stale();
            });
        }
        finally
        {
            if (hash is not null) CryptographicOperations.ZeroMemory(hash);
        }
    }

    public Task SaveTrainingAsync(Training? original, string name, int roomId, WorkoutType type, int coachId,
        int capacity, int weekday, TimeSpan start, TimeSpan end)
    {
        Require(StaffRole.Admin);
        name = Required(name, 100, "Тренировка");
        if (capacity <= 0 || weekday is < 1 or > 7 || !Enum.IsDefined(type) ||
            start < TimeSpan.Zero || end >= TimeSpan.FromDays(1) || end - start < TimeSpan.FromMinutes(1) ||
            start.Ticks % TimeSpan.TicksPerMinute != 0 || end.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new FitnessRuleException("Проверьте день недели, время и лимит участников.");

        return WriteAsync(async db =>
        {
            int roomCapacity = await db.ScalarAsync<int>("SELECT Capacity FROM dbo.Rooms WHERE Id = @id;", ("@id", roomId));
            if (capacity > roomCapacity)
                throw new FitnessRuleException("Лимит участников превышает вместимость зала.");
            if (await db.ScalarAsync<int>("""
                SELECT TOP (1) 1 FROM dbo.Employees WHERE Id = @id AND Role = N'Coach' AND IsActive = 1;
                """, ("@id", coachId)) == 0)
                throw new FitnessRuleException("Выберите действующего тренера.");
            if (await db.ScalarAsync<int>("""
                SELECT TOP (1) 1 FROM dbo.Trainings WHERE Id <> @id AND Weekday = @weekday
                    AND (RoomId = @room OR CoachId = @coach) AND StartsAt < @end AND EndsAt > @start;
                """, ("@id", original?.Id ?? 0), ("@weekday", weekday), ("@room", roomId), ("@coach", coachId),
                ("@start", start), ("@end", end)) == 1)
                throw new FitnessRuleException("Зал или тренер уже заняты в это время.");

            if (original is not null)
            {
                int bookings = await db.ScalarAsync<int>("""
                    SELECT COALESCE(MAX(t.Total), 0) FROM (
                        SELECT COUNT(*) AS Total FROM dbo.Bookings WHERE TrainingId = @id GROUP BY SessionOn
                    ) t;
                    """, ("@id", original.Id));
                if (bookings > capacity || (bookings > 0 && (original.Weekday != weekday ||
                    original.StartsAt != start || original.EndsAt != end || original.CoachId != coachId ||
                    original.RoomId != roomId)))
                    throw new FitnessRuleException("По тренировке есть записи. Сохраняйте её время, зал, тренера и достаточный лимит мест.");
            }

            string sql = original is null
                ? """
                    INSERT dbo.Trainings (Name, RoomId, Type, CoachId, MaxParticipants, Weekday, StartsAt, EndsAt)
                    VALUES (@name, @room, @type, @coach, @capacity, @weekday, @start, @end);
                    """
                : """
                    UPDATE dbo.Trainings SET Name = @name, RoomId = @room, Type = @type, CoachId = @coach,
                        MaxParticipants = @capacity, Weekday = @weekday, StartsAt = @start, EndsAt = @end
                    WHERE Id = @id AND Version = @version;
                    """;
            if (await db.ExecuteAsync(sql, ("@name", name), ("@room", roomId), ("@type", type), ("@coach", coachId),
                ("@capacity", capacity), ("@weekday", weekday), ("@start", start), ("@end", end),
                ("@id", original?.Id), ("@version", original?.Version)) == 0) throw Stale();
        });
    }

    public Task<ClubReport> ReportAsync(DateTime start, DateTime end)
    {
        Require(StaffRole.Admin);
        if (start.Date > end.Date || end.Year > 2099)
            throw new FitnessRuleException("Проверьте период отчёта.");
        return ReadAsync(async db => (await db.ReadAsync("""
            SELECT (SELECT COUNT(*) FROM dbo.Clients),
                   (SELECT COUNT(*) FROM dbo.Memberships WHERE State = N'Active' AND @today BETWEEN StartsOn AND EndsOn),
                   COUNT(*), COUNT(CASE WHEN AccessType = N'Group' THEN 1 END),
                   COUNT(CASE WHEN AccessType = N'Guest' OR b.IsGuest = 1 THEN 1 END), COALESCE(SUM(v.Price), 0)
            FROM dbo.Visits v LEFT JOIN dbo.Bookings b ON b.Id = v.BookingId
            WHERE v.EnteredAt >= @start AND v.EnteredAt < @end;
            """, reader => new ClubReport(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
                reader.GetInt32(3), reader.GetInt32(4), reader.GetDecimal(5)),
            ("@today", DateTime.Today), ("@start", start.Date), ("@end", end.Date.AddDays(1)))).Single());
    }

    private static Employee ReadEmployee(SqlDataReader reader)
        => new(reader.GetInt32(0), reader.GetString(1), Code<StaffRole>(reader, 2), reader.GetString(3),
            reader.GetString(4), reader.GetBoolean(5), reader.GetFieldValue<byte[]>(6));
}
