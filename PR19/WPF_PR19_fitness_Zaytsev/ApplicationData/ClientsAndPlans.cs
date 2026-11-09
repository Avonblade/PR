using Microsoft.Data.SqlClient;

namespace WPF_PR19_fitness_Zaytsev.ApplicationData;

internal sealed partial class FitnessRepository
{
    private const string PlanColumns = """
        m.Id, m.ClientId, m.Type, m.StartsOn, m.EndsOn, m.VisitLimit,
        (SELECT COUNT(*) FROM dbo.Visits v WHERE v.MembershipId = m.Id), m.State, m.CancelledOn
        """;

    public Task<List<ClientRow>> ClientsAsync()
    {
        Require(StaffRole.Admin, StaffRole.Sales, StaffRole.Desk);
        return ReadAsync(db => db.ReadAsync($"""
            SELECT c.Id, c.FullName, c.BirthDate, c.Phone, c.Email, c.Card, c.Status, c.Version,
                   {PlanColumns}, recent.LastVisit
            FROM dbo.Clients c
            OUTER APPLY (
                SELECT TOP (1) p.* FROM dbo.Memberships p
                WHERE p.ClientId = c.Id
                ORDER BY CASE
                    WHEN p.State = N'Active' AND @today BETWEEN p.StartsOn AND p.EndsOn THEN 0
                    WHEN p.State = N'Frozen' AND @today BETWEEN p.StartsOn AND p.EndsOn THEN 1
                    WHEN p.State <> N'Cancelled' AND p.StartsOn <= @today THEN 2
                    WHEN p.State <> N'Cancelled' THEN 3 ELSE 4 END,
                    CASE WHEN p.StartsOn > @today THEN p.StartsOn END,
                    p.EndsOn DESC, p.Id DESC
            ) m
            OUTER APPLY (SELECT MAX(v.EnteredAt) AS LastVisit FROM dbo.Visits v WHERE v.ClientId = c.Id) recent
            ORDER BY c.FullName, c.Id;
            """, reader => new ClientRow(ReadClient(reader), reader.IsDBNull(8) ? null : ReadPlan(reader, 8), Date(reader, 17)),
            ("@today", DateTime.Today)));
    }

    public Task SaveClientAsync(ClientProfile? original, string name, DateTime birthDate, string phone,
        string? email, string? card, ClientStatus status)
    {
        var session = Require(StaffRole.Admin, StaffRole.Sales);
        if (original is null && !session.Admin)
            throw new FitnessRuleException("Добавлять клиентов может только администратор.");
        phone = Required(phone, 25, "Телефон");
        email = Optional(email, 100, "Электронная почта");
        if (session.Admin)
        {
            name = Required(name, 100, "ФИО");
            card = Optional(card, 30, "Карта");
            if (birthDate.Date > DateTime.Today || birthDate.Year < 1900 || !Enum.IsDefined(status))
                throw new FitnessRuleException("Проверьте дату рождения и статус клиента.");
        }

        return WriteAsync(async db =>
        {
            if (original is null)
            {
                await db.ExecuteAsync("""
                    INSERT dbo.Clients (FullName, BirthDate, Phone, Email, Card, Status)
                    VALUES (@name, @birth, @phone, @email, @card, @status);
                    """, ("@name", name), ("@birth", birthDate.Date), ("@phone", phone),
                    ("@email", email), ("@card", card), ("@status", status));
                return;
            }

            int changed = session.Admin
                ? await db.ExecuteAsync("""
                    UPDATE dbo.Clients SET FullName = @name, BirthDate = @birth, Phone = @phone,
                        Email = @email, Card = @card, Status = @status WHERE Id = @id AND Version = @version;
                    """, ("@name", name), ("@birth", birthDate.Date), ("@phone", phone), ("@email", email),
                    ("@card", card), ("@status", status), ("@id", original.Id), ("@version", original.Version))
                : await db.ExecuteAsync("""
                    UPDATE dbo.Clients SET Phone = @phone, Email = @email WHERE Id = @id AND Version = @version;
                    """, ("@phone", phone), ("@email", email), ("@id", original.Id), ("@version", original.Version));
            if (changed == 0) throw Stale();
        });
    }

    public Task ToggleBlockAsync(ClientProfile client)
    {
        Require(StaffRole.Admin);
        return WriteAsync(async db =>
        {
            var status = client.Status == ClientStatus.Blocked ? ClientStatus.Active : ClientStatus.Blocked;
            if (await db.ExecuteAsync("UPDATE dbo.Clients SET Status = @status WHERE Id = @id AND Version = @version;",
                ("@status", status), ("@id", client.Id), ("@version", client.Version)) == 0)
                throw Stale();
        });
    }

    public Task<List<Membership>> PlansAsync(int clientId)
    {
        Require(StaffRole.Admin, StaffRole.Sales, StaffRole.Desk);
        return ReadAsync(db => db.ReadAsync($"""
            SELECT {PlanColumns} FROM dbo.Memberships m WHERE m.ClientId = @id ORDER BY m.StartsOn DESC, m.Id DESC;
            """, reader => ReadPlan(reader, 0), ("@id", clientId)));
    }

    internal static DateTime PlanEnd(PlanType type, DateTime startsOn)
        => type switch
        {
            PlanType.Single => startsOn.Date,
            PlanType.Month => startsOn.Date.AddMonths(1).AddDays(-1),
            PlanType.Quarter => startsOn.Date.AddMonths(3).AddDays(-1),
            PlanType.Year => startsOn.Date.AddYears(1).AddDays(-1),
            _ => throw new FitnessRuleException("Выберите тип абонемента.")
        };

    public Task SellAsync(int clientId, PlanType type, DateTime startsOn, int? limit)
    {
        Require(StaffRole.Admin, StaffRole.Sales);
        startsOn = startsOn.Date;
        if (startsOn < DateTime.Today)
            throw new FitnessRuleException("Дата начала в прошлом. Выберите сегодня или более позднюю дату.");
        if (startsOn.Year > 2099)
            throw new FitnessRuleException("Введите дату начала до 2100 года.");
        if (limit is <= 0 || (type == PlanType.Single && limit != 1))
            throw new FitnessRuleException("Лимит должен быть целым положительным числом. Для разового абонемента — 1.");
        DateTime endsOn = PlanEnd(type, startsOn);

        return WriteAsync(async db =>
        {
            var client = await GetClientAsync(db, clientId);
            if (client.Status == ClientStatus.Blocked)
                throw new FitnessRuleException("Клиент заблокирован. Сначала разблокируйте карточку.");
            if (await OverlapsAsync(db, clientId, startsOn, endsOn))
                throw new FitnessRuleException("Этот период пересекается с другим абонементом. Для продления начните со следующего дня после его окончания.");
            await db.ExecuteAsync("""
                INSERT dbo.Memberships (ClientId, Type, StartsOn, EndsOn, VisitLimit, State)
                VALUES (@client, @type, @start, @end, @limit, N'Active');
                """, ("@client", clientId), ("@type", type), ("@start", startsOn),
                ("@end", endsOn), ("@limit", limit));
        });
    }

    public Task FreezeAsync(int membershipId, DateTime startsOn, DateTime endsOn, FreezeReason reason)
    {
        Require(StaffRole.Admin, StaffRole.Sales);
        startsOn = startsOn.Date;
        endsOn = endsOn.Date;
        if (startsOn > endsOn || endsOn.Year > 2099 || !Enum.IsDefined(reason))
            throw new FitnessRuleException("Проверьте период и причину заморозки.");

        return WriteAsync(async db =>
        {
            var plan = await GetPlanAsync(db, membershipId);
            if (plan.StateOn(DateTime.Today) != PlanState.Active)
                throw new FitnessRuleException("Заморозить можно только действующий активный абонемент.");
            if (startsOn <= plan.EndsOn)
                throw new FitnessRuleException("Период заморозки не должен пересекаться со сроком абонемента. Начните после его окончания.");
            DateTime extended = plan.EndsOn.AddDays((endsOn - startsOn).Days + 1);
            if (await OverlapsAsync(db, plan.ClientId, plan.StartsOn, extended, membershipId) ||
                await OverlapsAsync(db, plan.ClientId, startsOn, endsOn, membershipId))
                throw new FitnessRuleException("Заморозка пересекается с другим абонементом клиента.");

            await db.ExecuteAsync("""
                UPDATE dbo.Memberships SET State = N'Frozen', EndsOn = @extended WHERE Id = @id;
                INSERT dbo.Freezes (MembershipId, StartsOn, EndsOn, Reason)
                VALUES (@id, @start, @end, @reason);
                """, ("@id", membershipId), ("@extended", extended), ("@start", startsOn),
                ("@end", endsOn), ("@reason", reason));
        });
    }

    public Task ThawAsync(int membershipId)
    {
        Require(StaffRole.Admin);
        return WriteAsync(async db =>
        {
            var plan = await GetPlanAsync(db, membershipId);
            if (plan.StateOn(DateTime.Today) != PlanState.Frozen)
                throw new FitnessRuleException("Выберите замороженный действующий абонемент.");
            await db.ExecuteAsync("""
                UPDATE dbo.Memberships SET State = N'Active' WHERE Id = @id;
                UPDATE dbo.Freezes SET ReleasedOn = @today WHERE MembershipId = @id AND ReleasedOn IS NULL;
                """, ("@id", membershipId), ("@today", DateTime.Today));
        });
    }

    public Task CancelAsync(int membershipId)
    {
        Require(StaffRole.Admin);
        return WriteAsync(async db =>
        {
            var plan = await GetPlanAsync(db, membershipId);
            if (plan.StateOn(DateTime.Today) is PlanState.Cancelled or PlanState.Expired)
                throw new FitnessRuleException("Абонемент уже отменён или истёк.");
            await db.ExecuteAsync("""
                UPDATE dbo.Memberships SET State = N'Cancelled', CancelledOn = @today WHERE Id = @id;
                UPDATE dbo.Freezes SET ReleasedOn = @today WHERE MembershipId = @id AND ReleasedOn IS NULL;
                """, ("@id", membershipId), ("@today", DateTime.Today));
        });
    }

    private static async Task<bool> OverlapsAsync(FitnessDatabase db, int clientId, DateTime start, DateTime end, int exclude = 0)
        => await db.ScalarAsync<int>("""
            SELECT TOP (1) 1 FROM dbo.Memberships
            WHERE ClientId = @client AND State <> N'Cancelled' AND Id <> @exclude
              AND StartsOn <= @end AND EndsOn >= @start;
            """, ("@client", clientId), ("@start", start), ("@end", end), ("@exclude", exclude)) == 1;

    private static async Task<ClientProfile> GetClientAsync(FitnessDatabase db, int id)
        => (await db.ReadAsync("""
            SELECT Id, FullName, BirthDate, Phone, Email, Card, Status, Version FROM dbo.Clients WHERE Id = @id;
            """, ReadClient, ("@id", id))).SingleOrDefault() ?? throw Stale();

    private static async Task<Membership> GetPlanAsync(FitnessDatabase db, int id)
        => (await db.ReadAsync($"SELECT {PlanColumns} FROM dbo.Memberships m WHERE m.Id = @id;",
            reader => ReadPlan(reader, 0), ("@id", id))).SingleOrDefault() ?? throw Stale();

    private static async Task<Membership?> ActivePlanAsync(FitnessDatabase db, int clientId, DateTime day)
        => (await db.ReadAsync($"""
            SELECT {PlanColumns} FROM dbo.Memberships m
            WHERE m.ClientId = @client AND m.State = N'Active' AND @day BETWEEN m.StartsOn AND m.EndsOn;
            """, reader => ReadPlan(reader, 0), ("@client", clientId), ("@day", day.Date))).SingleOrDefault();

    private static ClientProfile ReadClient(SqlDataReader reader)
        => new(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2), reader.GetString(3),
            Text(reader, 4), Text(reader, 5), Code<ClientStatus>(reader, 6), reader.GetFieldValue<byte[]>(7));

    private static Membership ReadPlan(SqlDataReader reader, int i)
        => new(reader.GetInt32(i), reader.GetInt32(i + 1), Code<PlanType>(reader, i + 2), reader.GetDateTime(i + 3),
            reader.GetDateTime(i + 4), Number(reader, i + 5), reader.GetInt32(i + 6),
            Code<PlanState>(reader, i + 7), Date(reader, i + 8));
}
