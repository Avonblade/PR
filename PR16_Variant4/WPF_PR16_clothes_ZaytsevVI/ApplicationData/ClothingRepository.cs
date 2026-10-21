using System.Data;
using Microsoft.Data.SqlClient;

namespace WPF_PR16_clothes_ZaytsevVI.ApplicationData;

public sealed class ClothingRepository
{
    public Task<List<LookupItem>> GetCategoriesAsync() =>
        GetLookupAsync("SELECT Id, Name FROM dbo.ClothingCategory ORDER BY Name");

    public Task<List<LookupItem>> GetMaterialsAsync() =>
        GetLookupAsync("SELECT Id, Name FROM dbo.ClothingMaterial ORDER BY Name");

    public Task<List<LookupItem>> GetSizesAsync() =>
        GetLookupAsync("SELECT Id, Name FROM dbo.ClothingSize ORDER BY TRY_CONVERT(int,Name), Name");

    public Task<List<LookupItem>> GetColorsAsync() =>
        GetLookupAsync("SELECT Id, Name FROM dbo.ClothingColor ORDER BY Name");

    private static async Task<List<LookupItem>> GetLookupAsync(string sql)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var items = new List<LookupItem>();
        while (await reader.ReadAsync())
            items.Add(new LookupItem(reader.GetInt32(0), reader.GetString(1)));
        return items;
    }

    public async Task<List<ClothingProduct>> GetProductsAsync()
    {
        const string sql = """
            SELECT p.Id, p.Article, p.Name, p.CategoryId, p.MaterialId, p.SizeId, p.ColorId,
                   c.Name, m.Name, s.Name, col.Name, p.Price, p.Stock, p.Description, p.Version
            FROM dbo.ClothingProduct p
            JOIN dbo.ClothingCategory c ON c.Id = p.CategoryId
            JOIN dbo.ClothingMaterial m ON m.Id = p.MaterialId
            JOIN dbo.ClothingSize s ON s.Id = p.SizeId
            JOIN dbo.ClothingColor col ON col.Id = p.ColorId
            ORDER BY p.Id;
            """;
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var products = new List<ClothingProduct>();
        while (await reader.ReadAsync())
        {
            products.Add(new ClothingProduct
            {
                Id = reader.GetInt32(0), Article = reader.GetString(1), Name = reader.GetString(2),
                CategoryId = reader.GetInt32(3), MaterialId = reader.GetInt32(4),
                SizeId = reader.GetInt32(5), ColorId = reader.GetInt32(6),
                CategoryName = reader.GetString(7), MaterialName = reader.GetString(8),
                SizeName = reader.GetString(9), ColorName = reader.GetString(10),
                Price = reader.GetDecimal(11), Stock = reader.GetInt32(12),
                Description = reader.GetString(13), Version = (byte[])reader.GetValue(14)
            });
        }
        return products;
    }

    public async Task<int> SaveAsync(ClothingProduct product)
    {
        Validate(product);
        string sql = product.Id == 0
            ? """
              INSERT dbo.ClothingProduct
                  (Article, Name, CategoryId, MaterialId, SizeId, ColorId, Price, Stock, Description)
              OUTPUT INSERTED.Id
              VALUES (@Article, @Name, @CategoryId, @MaterialId, @SizeId, @ColorId, @Price, @Stock, @Description);
              """
            : """
              UPDATE dbo.ClothingProduct
              SET Article=@Article, Name=@Name, CategoryId=@CategoryId, MaterialId=@MaterialId,
                  SizeId=@SizeId, ColorId=@ColorId, Price=@Price, Stock=@Stock, Description=@Description
              OUTPUT INSERTED.Id
              WHERE Id=@Id AND Version=@Version;
              """;
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Article", SqlDbType.NVarChar, 30).Value = product.Article.Trim();
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 120).Value = product.Name.Trim();
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = product.CategoryId;
        command.Parameters.Add("@MaterialId", SqlDbType.Int).Value = product.MaterialId;
        command.Parameters.Add("@SizeId", SqlDbType.Int).Value = product.SizeId;
        command.Parameters.Add("@ColorId", SqlDbType.Int).Value = product.ColorId;
        var price = command.Parameters.Add("@Price", SqlDbType.Decimal);
        price.Precision = 12;
        price.Scale = 2;
        price.Value = product.Price;
        command.Parameters.Add("@Stock", SqlDbType.Int).Value = product.Stock;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 2000).Value = product.Description.Trim();
        if (product.Id != 0)
        {
            command.Parameters.Add("@Id", SqlDbType.Int).Value = product.Id;
            command.Parameters.Add("@Version", SqlDbType.Timestamp, 8).Value = product.Version;
        }
        object? id = await command.ExecuteScalarAsync();
        if (id is null)
            throw new InvalidOperationException("Товар изменён или удалён в другом окне. Закройте форму, обновите каталог и повторите действие.");
        return Convert.ToInt32(id);
    }

    public async Task DeleteAsync(ClothingProduct product)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(
            "DELETE dbo.ClothingProduct WHERE Id=@Id AND Version=@Version", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = product.Id;
        command.Parameters.Add("@Version", SqlDbType.Timestamp, 8).Value = product.Version;
        if (await command.ExecuteNonQueryAsync() == 0)
            throw new InvalidOperationException("Товар уже изменён или удалён. Обновите каталог и повторите действие.");
    }

    public static void Validate(ClothingProduct product)
    {
        if (string.IsNullOrWhiteSpace(product.Article) || product.Article.Trim().Length > 30)
            throw new ArgumentException("Артикул должен содержать от 1 до 30 символов.");
        if (string.IsNullOrWhiteSpace(product.Name) || product.Name.Trim().Length > 120)
            throw new ArgumentException("Название должно содержать от 1 до 120 символов.");
        if (product.CategoryId <= 0 || product.MaterialId <= 0 || product.SizeId <= 0 || product.ColorId <= 0)
            throw new ArgumentException("Выберите категорию, материал, размер и цвет.");
        if (product.Price < 0 || product.Price > 9999999999.99m || decimal.Round(product.Price, 2) != product.Price)
            throw new ArgumentException("Цена: от 0 до 9 999 999 999,99 ₽, не более двух знаков после запятой.");
        if (product.Stock < 0 || product.Stock > 1000000)
            throw new ArgumentException("Количество на складе: от 0 до 1 000 000 шт.");
        if (product.Description.Length > 2000)
            throw new ArgumentException("Описание не должно превышать 2000 символов.");
    }
}
