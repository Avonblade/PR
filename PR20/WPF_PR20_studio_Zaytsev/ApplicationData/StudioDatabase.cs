using System.Data;
using Microsoft.Data.SqlClient;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed class StudioDatabase(SqlConnection connection, SqlTransaction? transaction = null)
{
    public async Task<List<T>> ReadAsync<T>(string sql, Func<SqlDataReader, T> project,
        params (string Name, object? Value)[] values)
    {
        using var command = Create(sql, values);
        await using var reader = await command.ExecuteReaderAsync();
        var result = new List<T>();
        while (await reader.ReadAsync()) result.Add(project(reader));
        return result;
    }

    public async Task<int> ExecuteAsync(string sql, params (string Name, object? Value)[] values)
    {
        using var command = Create(sql, values);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql, params (string Name, object? Value)[] values)
    {
        using var command = Create(sql, values);
        object? result = await command.ExecuteScalarAsync();
        return result is T typed ? typed : default;
    }

    public async Task<DataTable> TableAsync(string sql, params (string Name, object? Value)[] values)
    {
        using var command = Create(sql, values);
        await using var reader = await command.ExecuteReaderAsync();
        var table = new DataTable();
        for (int i = 0; i < reader.FieldCount; i++) table.Columns.Add(reader.GetName(i), reader.GetFieldType(i));
        var cells = new object[reader.FieldCount];
        while (await reader.ReadAsync())
        {
            reader.GetValues(cells);
            table.Rows.Add(cells);
        }
        return table;
    }

    private SqlCommand Create(string sql, (string Name, object? Value)[] values)
    {
        var command = new SqlCommand(sql, connection, transaction);
        foreach (var (name, value) in values)
        {
            switch (value)
            {
                case DateTime date:
                    command.Parameters.Add(name, SqlDbType.DateTime2).Value = date;
                    break;
                case TimeSpan time:
                    command.Parameters.Add(name, SqlDbType.Time).Value = time;
                    break;
                case Enum item:
                    command.Parameters.Add(name, SqlDbType.NVarChar, 12).Value = item.ToString();
                    break;
                default:
                    command.Parameters.AddWithValue(name, value ?? DBNull.Value);
                    break;
            }
        }
        return command;
    }
}
