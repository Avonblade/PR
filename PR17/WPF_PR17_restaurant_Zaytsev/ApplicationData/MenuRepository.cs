using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace WPF_PR17_restaurant_Zaytsev.ApplicationData;

internal sealed class MenuRepository
{
    private const int PasswordIterations = 600_000;
    public string? EditorName { get; private set; }

    public async Task<MenuData> LoadAsync()
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        using var command = new SqlCommand("""
            SELECT Id, Name FROM dbo.Categories ORDER BY Name;
            SELECT Id, Name FROM dbo.Cuisines ORDER BY Name;
            SELECT Id, Name FROM dbo.ServingSizes ORDER BY Id;
            SELECT d.Id, d.Name, c.Id, c.Name, k.Id, k.Name, s.Id, s.Name,
                   d.Price, d.Description, d.Version
            FROM dbo.Dishes d
            JOIN dbo.Categories c ON c.Id = d.CategoryId
            JOIN dbo.Cuisines k ON k.Id = d.CuisineId
            JOIN dbo.ServingSizes s ON s.Id = d.ServingSizeId
            ORDER BY d.Name, d.Id;
            """, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var categories = await ReadLookupAsync(reader);
        await reader.NextResultAsync();
        var cuisines = await ReadLookupAsync(reader);
        await reader.NextResultAsync();
        var sizes = await ReadLookupAsync(reader);
        await reader.NextResultAsync();
        List<Dish> dishes = [];
        while (await reader.ReadAsync())
            dishes.Add(new Dish(reader.GetInt32(0), reader.GetString(1),
                ReadLookup(reader, 2), ReadLookup(reader, 4), ReadLookup(reader, 6),
                reader.GetDecimal(8), reader.GetString(9), reader.GetFieldValue<byte[]>(10)));
        return new MenuData(dishes, categories, cuisines, sizes);
    }

    private static LookupItem ReadLookup(SqlDataReader reader, int offset)
        => new(reader.GetInt32(offset), reader.GetString(offset + 1));

    private static async Task<List<LookupItem>> ReadLookupAsync(SqlDataReader reader)
    {
        List<LookupItem> items = [];
        while (await reader.ReadAsync())
            items.Add(ReadLookup(reader, 0));
        return items;
    }

    public async Task<bool> SignInAsync(string login, string password)
    {
        EditorName = null;
        string? name = null;
        byte[] salt = new byte[16];
        byte[] expectedHash = new byte[32];
        await using (var connection = await DatabaseConnection.OpenAsync())
        {
            using var command = new SqlCommand("""
                SELECT DisplayName, PasswordSalt, PasswordHash
                FROM dbo.Users WHERE Login = @login AND IsActive = 1;
                """, connection);
            command.Parameters.Add("@login", SqlDbType.NVarChar, 50).Value = login;
            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                name = reader.GetString(0);
                salt = reader.GetFieldValue<byte[]>(1);
                expectedHash = reader.GetFieldValue<byte[]>(2);
            }
        }

        byte[] actualHash = await Task.Run(() =>
            Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32));
        bool matches = CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        CryptographicOperations.ZeroMemory(actualHash);
        EditorName = matches ? name : null;
        return EditorName is not null;
    }

    public void SignOut() => EditorName = null;

    public async Task<int> SaveAsync(DishInput input, Dish? original)
    {
        EnsureEditor();
        await using var connection = await DatabaseConnection.OpenAsync();
        using var command = connection.CreateCommand();
        command.CommandText = original is null
            ? """
                INSERT dbo.Dishes (Name, CategoryId, CuisineId, ServingSizeId, Price, Description)
                OUTPUT INSERTED.Id
                VALUES (@name, @category, @cuisine, @size, @price, @description);
                """
            : """
                UPDATE dbo.Dishes
                SET Name = @name, CategoryId = @category, CuisineId = @cuisine,
                    ServingSizeId = @size, Price = @price, Description = @description
                OUTPUT INSERTED.Id
                WHERE Id = @id AND Version = @version;
                """;
        command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = input.Name;
        command.Parameters.Add("@category", SqlDbType.Int).Value = input.CategoryId;
        command.Parameters.Add("@cuisine", SqlDbType.Int).Value = input.CuisineId;
        command.Parameters.Add("@size", SqlDbType.Int).Value = input.ServingSizeId;
        command.Parameters.Add("@description", SqlDbType.NVarChar, 500).Value = input.Description;
        var price = command.Parameters.Add("@price", SqlDbType.Decimal);
        price.Precision = 9;
        price.Scale = 2;
        price.Value = input.Price;
        if (original is not null)
            AddVersionParameters(command, original);
        return await command.ExecuteScalarAsync() is int id ? id : throw StaleDish();
    }

    public async Task DeleteAsync(Dish dish)
    {
        EnsureEditor();
        await using var connection = await DatabaseConnection.OpenAsync();
        using var command = new SqlCommand(
            "DELETE dbo.Dishes WHERE Id = @id AND Version = @version;", connection);
        AddVersionParameters(command, dish);
        if (await command.ExecuteNonQueryAsync() == 0)
            throw StaleDish();
    }

    private void EnsureEditor()
    {
        if (EditorName is null)
            throw new InvalidOperationException("Для изменения меню войдите в систему.");
    }

    private static void AddVersionParameters(SqlCommand command, Dish dish)
    {
        command.Parameters.Add("@id", SqlDbType.Int).Value = dish.Id;
        command.Parameters.Add("@version", SqlDbType.Binary, 8).Value = dish.Version;
    }

    private static InvalidOperationException StaleDish()
        => new("Блюдо уже изменено или удалено. Обновите меню и повторите действие.");
}
