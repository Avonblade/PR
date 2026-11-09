namespace WPF_PR19_fitness_Zaytsev.ApplicationData;

internal sealed partial class FitnessRepository
{
    public Task<List<VisitRow>> VisitsAsync(int? clientId = null, bool openOnly = false)
    {
        Require(StaffRole.Admin, StaffRole.Sales, StaffRole.Desk);
        return ReadAsync(db => db.ReadAsync("""
            SELECT v.Id, v.ClientId, c.FullName, v.EnteredAt, v.ExitedAt, v.AccessType, v.Price, e.FullName
            FROM dbo.Visits v JOIN dbo.Clients c ON c.Id = v.ClientId
            LEFT JOIN dbo.Employees e ON e.Id = v.CoachId
            WHERE (@client IS NULL OR v.ClientId = @client) AND (@open = 0 OR v.ExitedAt IS NULL)
            ORDER BY v.EnteredAt DESC, v.Id DESC;
            """, reader => new VisitRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2),
                reader.GetDateTime(3), Date(reader, 4), Code<AccessType>(reader, 5),
                reader.IsDBNull(6) ? null : reader.GetDecimal(6), Text(reader, 7)),
            ("@client", clientId), ("@open", openOnly)));
    }

    public Task EnterAsync(int clientId, AccessType type, decimal? price)
    {
        Require(StaffRole.Admin, StaffRole.Desk);
        if (type == AccessType.Group || !Enum.IsDefined(type))
            throw new FitnessRuleException("Выберите тип входа на стойке.");
        if (type == AccessType.Single && (price is null or <= 0 or > 9_999_999.99m ||
            decimal.Round(price.Value, 2) != price))
            throw new FitnessRuleException("Укажите положительную стоимость разового входа с точностью до копеек.");

        return WriteAsync(async db =>
        {
            DateTime now = Now();
            var client = await GetClientAsync(db, clientId);
            if (client.Status == ClientStatus.Blocked)
                throw new FitnessRuleException("Клиент заблокирован. Вход запрещён.");
            if (await db.ScalarAsync<int>("""
                SELECT TOP (1) 1 FROM dbo.Visits WHERE ClientId = @id AND ExitedAt IS NULL AND AccessType <> N'Group';
                """, ("@id", clientId)) == 1)
                throw new FitnessRuleException("У клиента уже есть открытое посещение. Сначала зафиксируйте выход.");

            int? membership = null;
            if (type == AccessType.Membership)
            {
                var plan = await ActivePlanAsync(db, clientId, now.Date);
                if (client.Status != ClientStatus.Active || plan is null)
                    throw new FitnessRuleException("Нет доступного активного абонемента. Выберите разовый или гостевой вход либо оформите абонемент.");
                if (plan.Remaining == 0)
                    throw new FitnessRuleException("Остаток посещений — 0. Выберите разовый вход или обратитесь к менеджеру для продления.");
                membership = plan.Id;
            }

            await db.ExecuteAsync("""
                INSERT dbo.Visits (ClientId, MembershipId, EnteredAt, AccessType, Price)
                VALUES (@client, @membership, @now, @type, @price);
                """, ("@client", clientId), ("@membership", membership), ("@now", now),
                ("@type", type), ("@price", type == AccessType.Single ? price : null));
        });
    }

    public Task ExitAsync(int visitId, DateTime exitAt)
    {
        Require(StaffRole.Admin, StaffRole.Desk);
        return WriteAsync(async db =>
        {
            var visits = await db.ReadAsync("SELECT EnteredAt, ExitedAt FROM dbo.Visits WHERE Id = @id;",
                reader => (Entered: reader.GetDateTime(0), Exited: Date(reader, 1)), ("@id", visitId));
            if (visits.Count == 0 || visits[0].Exited is not null)
                throw new FitnessRuleException("Посещение уже закрыто или не найдено. Обновите список.");
            if (exitAt < visits[0].Entered)
                throw new FitnessRuleException("Время выхода не может быть раньше времени входа.");
            if (exitAt > Now())
                throw new FitnessRuleException("Время выхода не может быть в будущем.");

            await db.ExecuteAsync("UPDATE dbo.Visits SET ExitedAt = @exit WHERE Id = @id;",
                ("@exit", exitAt), ("@id", visitId));
        });
    }

    private static DateTime Now()
    {
        DateTime now = DateTime.Now;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
    }
}
