using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace WPF_PR18_insurance_Zaytsev.ApplicationData;

internal sealed class InsuranceRepository
{
    public StaffSession? Session { get; private set; }

    public async Task<bool> SignInAsync(string login, string password)
    {
        Session = null;
        StaffSession? candidate = null;
        byte[] salt = new byte[16];
        byte[] expectedHash = new byte[32];
        await using (var connection = await DatabaseConnection.OpenAsync())
        {
            using var command = new SqlCommand("""
                SELECT FullName, Role, PasswordSalt, PasswordHash
                FROM dbo.Users WHERE Login = @login AND IsActive = 1;
                """, connection);
            command.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = login;
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                candidate = new StaffSession(reader.GetString(0), Enum.Parse<StaffRole>(reader.GetString(1)));
                salt = reader.GetFieldValue<byte[]>(2);
                expectedHash = reader.GetFieldValue<byte[]>(3);
            }
        }

        byte[] actualHash = await Task.Run(() =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, 600_000, HashAlgorithmName.SHA256, 32));
        Session = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash) ? candidate : null;
        CryptographicOperations.ZeroMemory(actualHash);
        return Session is not null;
    }

    public void SignOut() => Session = null;

    public async Task<List<RecordRow>> LoadAsync(Entity entity)
    {
        EnsureAccess(entity);
        var columns = new List<string> { "r.Id", "r.Version" };
        var joins = new List<string>();
        foreach (var field in entity.Fields)
        {
            if (field.Lookup is null)
            {
                columns.Add($"r.[{field.Name}]");
                continue;
            }
            string alias = $"lookup{joins.Count}";
            columns.Add($"{alias}.Caption");
            joins.Add($"LEFT JOIN ({field.Lookup}) {alias} ON {alias}.Id = r.[{field.Name}]");
        }
        if (entity.Summary is not null)
            columns.AddRange(entity.Summary.Select(column => $"r.[{column.Name}]"));

        await using var connection = await DatabaseConnection.OpenAsync();
        using var command = new SqlCommand($"""
            SELECT {string.Join(", ", columns)}
            FROM dbo.[{entity.ReadSource}] r
            {string.Join(Environment.NewLine, joins)}
            ORDER BY r.Id DESC;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        List<RecordRow> rows = [];
        while (await reader.ReadAsync())
        {
            var values = ReadValues(reader, 2);
            for (int i = 0; i < entity.Fields.Length; i++)
            {
                if (entity.Fields[i].Kind == FieldKind.Flag)
                    values[i] = values[i] is true ? "Да" : "Нет";
            }
            rows.Add(new RecordRow(reader.GetInt32(0), reader.GetFieldValue<byte[]>(1), values));
        }
        return rows;
    }

    public async Task<EditData> LoadEditorAsync(Entity entity, RecordRow? row)
    {
        EnsureAccess(entity, write: true);
        await using var connection = await DatabaseConnection.OpenAsync();
        var values = new object?[entity.Fields.Length];
        if (row is not null)
        {
            using var command = new SqlCommand($"""
                SELECT {string.Join(", ", entity.Fields.Select(field => $"[{field.Name}]"))}
                FROM dbo.[{entity.Table}] WHERE Id = @id AND Version = @version;
                """, connection);
            AddVersion(command, row);
            await using var reader = await command.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) throw StaleRecord();
            values = ReadValues(reader, 0);
        }

        Dictionary<string, List<ChoiceItem>> lookups = [];
        foreach (var field in entity.Fields.Where(field => field.Lookup is not null))
        {
            bool selfReference = field.Name == "ParentClaimId";
            using var command = new SqlCommand($"""
                SELECT Id, Caption FROM ({field.Lookup}) choices
                {(selfReference && row is not null ? "WHERE Id <> @id" : "")}
                ORDER BY Caption, Id;
                """, connection);
            if (selfReference && row is not null)
                command.Parameters.Add("@id", SqlDbType.Int).Value = row.Id;

            await using var reader = await command.ExecuteReaderAsync();
            List<ChoiceItem> options = [];
            while (await reader.ReadAsync())
                options.Add(new ChoiceItem(reader.GetInt32(0), reader.GetString(1)));
            lookups.Add(field.Name, options);
        }
        return new EditData(values, lookups);
    }

    public async Task SaveAsync(Entity entity, RecordRow? row, object?[] values)
    {
        EnsureAccess(entity, write: true);
        string[] names = entity.Fields.Select(field => $"[{field.Name}]").ToArray();
        string[] parameters = names.Select((_, index) => $"@p{index}").ToArray();
        string sql = row is null
            ? $"INSERT dbo.[{entity.Table}] ({string.Join(", ", names)}) VALUES ({string.Join(", ", parameters)});"
            : $"UPDATE dbo.[{entity.Table}] SET {string.Join(", ", names.Zip(parameters, (name, param) => $"{name} = {param}"))} WHERE Id = @id AND Version = @version;";

        await using var connection = await DatabaseConnection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        using var command = new SqlCommand(sql, connection, transaction);
        if (row is not null) AddVersion(command, row);
        for (int i = 0; i < names.Length; i++)
            AddValue(command, parameters[i], entity.Fields[i], values[i]);

        await ExecuteAsync(command, transaction);
    }

    public async Task DeleteAsync(Entity entity, RecordRow row)
    {
        if (Session?.Role != StaffRole.Admin)
            throw new InvalidOperationException("Удалять записи может только администратор.");
        EnsureAccess(entity);
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        using var command = new SqlCommand(
            $"DELETE dbo.[{entity.Table}] WHERE Id = @id AND Version = @version;", connection, transaction);
        AddVersion(command, row);
        await ExecuteAsync(command, transaction);
    }

    private static async Task ExecuteAsync(SqlCommand command, SqlTransaction transaction)
    {
        if (await command.ExecuteNonQueryAsync() == 0)
            throw StaleRecord();

        using var check = new SqlCommand("SELECT TOP (1) Message FROM dbo.WorkflowErrors ORDER BY Priority;",
            command.Connection, transaction);
        if (await check.ExecuteScalarAsync() is string message)
            throw new InvalidOperationException(message);

        await transaction.CommitAsync();
    }

    private void EnsureAccess(Entity entity, bool write = false)
    {
        if (!InsuranceModel.Sections.Any(section => ReferenceEquals(section, entity)) ||
            Session is null || !entity.VisibleTo(Session.Role) || (write && !entity.EditableBy(Session.Role)))
            throw new InvalidOperationException("Для этой операции недостаточно прав.");
    }

    private static void AddVersion(SqlCommand command, RecordRow row)
    {
        command.Parameters.Add("@id", SqlDbType.Int).Value = row.Id;
        command.Parameters.Add("@version", SqlDbType.Binary, 8).Value = row.Version;
    }

    private static void AddValue(SqlCommand command, string parameterName, Field field, object? value)
    {
        var parameter = field.Kind switch
        {
            FieldKind.Date => command.Parameters.Add(parameterName, SqlDbType.Date),
            FieldKind.Money => command.Parameters.Add(parameterName, SqlDbType.Decimal),
            FieldKind.Lookup => command.Parameters.Add(parameterName, SqlDbType.Int),
            FieldKind.Flag => command.Parameters.Add(parameterName, SqlDbType.Bit),
            _ => command.Parameters.Add(parameterName, SqlDbType.NVarChar, field.Length)
        };
        if (field.Kind == FieldKind.Money)
        {
            parameter.Precision = 12;
            parameter.Scale = 2;
        }
        parameter.Value = value ?? DBNull.Value;
    }

    private static object?[] ReadValues(SqlDataReader reader, int first)
    {
        var values = new object?[reader.FieldCount - first];
        for (int i = first; i < reader.FieldCount; i++)
            values[i - first] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        return values;
    }

    private static InvalidOperationException StaleRecord()
        => new("Запись уже изменена или удалена. Обновите список и повторите действие.");
}
