using System.IO;
using Microsoft.Data.SqlClient;

namespace WPF_PR15_Mebel_ZaytsevVI.ApplicationData;

/// <summary>Creates SQL Server connections using the configuration beside the executable.</summary>
public static class DatabaseConnection
{
    public static async Task<SqlConnection> OpenAsync()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "connection.txt");
        if (!File.Exists(path))
            throw new InvalidOperationException("Не найден connection.txt рядом с приложением.");

        var connection = new SqlConnection((await File.ReadAllTextAsync(path)).Trim());
        try
        {
            await connection.OpenAsync();
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
