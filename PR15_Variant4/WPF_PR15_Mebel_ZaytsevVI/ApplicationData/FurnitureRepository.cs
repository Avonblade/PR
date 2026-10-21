using System.Data;
using Microsoft.Data.SqlClient;

namespace WPF_PR15_Mebel_ZaytsevVI.ApplicationData;

/// <summary>Reads furniture and lookup data; writes products with optimistic concurrency checks.</summary>
public sealed class FurnitureRepository
{
    public async Task<List<LookupItem>> GetCategoriesAsync() =>
        await GetLookupAsync("SELECT Id, Name FROM dbo.FurnitureCategory ORDER BY Name");

    public async Task<List<LookupItem>> GetMaterialsAsync() =>
        await GetLookupAsync("SELECT Id, Name FROM dbo.FurnitureMaterial ORDER BY Name");

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

    public async Task<List<FurnitureProduct>> GetProductsAsync()
    {
        const string sql = """
            SELECT p.Id, p.Article, p.Name, p.CategoryId, p.MaterialId,
                   c.Name, m.Name, p.Price, p.WidthMm, p.DepthMm, p.HeightMm,
                   p.Stock, p.Description, p.Version
            FROM dbo.FurnitureProduct p
            JOIN dbo.FurnitureCategory c ON c.Id = p.CategoryId
            JOIN dbo.FurnitureMaterial m ON m.Id = p.MaterialId
            ORDER BY p.Id;
            """;
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync();
        var products = new List<FurnitureProduct>();
        while (await reader.ReadAsync())
        {
            products.Add(new FurnitureProduct
            {
                Id = reader.GetInt32(0), Article = reader.GetString(1), Name = reader.GetString(2),
                CategoryId = reader.GetInt32(3), MaterialId = reader.GetInt32(4),
                CategoryName = reader.GetString(5), MaterialName = reader.GetString(6),
                Price = reader.GetDecimal(7), WidthMm = reader.GetInt32(8),
                DepthMm = reader.GetInt32(9), HeightMm = reader.GetInt32(10),
                Stock = reader.GetInt32(11), Description = reader.GetString(12),
                Version = (byte[])reader.GetValue(13)
            });
        }
        return products;
    }

    public async Task<int> SaveAsync(FurnitureProduct product)
    {
        Validate(product);
        string sql = product.Id == 0
            ? """
              INSERT dbo.FurnitureProduct
                  (Article, Name, CategoryId, MaterialId, Price, WidthMm, DepthMm, HeightMm, Stock, Description)
              OUTPUT INSERTED.Id
              VALUES (@Article, @Name, @CategoryId, @MaterialId, @Price, @Width, @Depth, @Height, @Stock, @Description);
              """
            : """
              UPDATE dbo.FurnitureProduct
              SET Article=@Article, Name=@Name, CategoryId=@CategoryId, MaterialId=@MaterialId,
                  Price=@Price, WidthMm=@Width, DepthMm=@Depth, HeightMm=@Height, Stock=@Stock,
                  Description=@Description
              OUTPUT INSERTED.Id
              WHERE Id=@Id AND Version=@Version;
              """;
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Article", SqlDbType.NVarChar, 30).Value = product.Article.Trim();
        command.Parameters.Add("@Name", SqlDbType.NVarChar, 120).Value = product.Name.Trim();
        command.Parameters.Add("@CategoryId", SqlDbType.Int).Value = product.CategoryId;
        command.Parameters.Add("@MaterialId", SqlDbType.Int).Value = product.MaterialId;
        var price = command.Parameters.Add("@Price", SqlDbType.Decimal);
        price.Precision = 12;
        price.Scale = 2;
        price.Value = product.Price;
        command.Parameters.Add("@Width", SqlDbType.Int).Value = product.WidthMm;
        command.Parameters.Add("@Depth", SqlDbType.Int).Value = product.DepthMm;
        command.Parameters.Add("@Height", SqlDbType.Int).Value = product.HeightMm;
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

    public async Task DeleteAsync(FurnitureProduct product)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var command = new SqlCommand(
            "DELETE dbo.FurnitureProduct WHERE Id=@Id AND Version=@Version", connection);
        command.Parameters.Add("@Id", SqlDbType.Int).Value = product.Id;
        command.Parameters.Add("@Version", SqlDbType.Timestamp, 8).Value = product.Version;
        if (await command.ExecuteNonQueryAsync() == 0)
            throw new InvalidOperationException("Товар уже изменён или удалён. Обновите каталог и повторите действие.");
    }

    public static void Validate(FurnitureProduct product)
    {
        if (string.IsNullOrWhiteSpace(product.Article) || product.Article.Trim().Length > 30)
            throw new ArgumentException("Артикул должен содержать от 1 до 30 символов.");
        if (string.IsNullOrWhiteSpace(product.Name) || product.Name.Trim().Length > 120)
            throw new ArgumentException("Название должно содержать от 1 до 120 символов.");
        if (product.CategoryId <= 0 || product.MaterialId <= 0)
            throw new ArgumentException("Выберите категорию и материал.");
        if (product.Price < 0 || product.Price > 9999999999.99m || decimal.Round(product.Price, 2) != product.Price)
            throw new ArgumentException("Цена: от 0 до 9 999 999 999,99 ₽, не более двух знаков после запятой.");
        if (product.WidthMm < 1 || product.WidthMm > 10000 || product.DepthMm < 1 || product.DepthMm > 10000 || product.HeightMm < 1 || product.HeightMm > 10000)
            throw new ArgumentException("Каждый размер должен быть целым числом от 1 до 10 000 мм.");
        if (product.Stock < 0 || product.Stock > 1000000)
            throw new ArgumentException("Количество на складе: от 0 до 1 000 000 шт.");
        if (product.Description.Length > 2000)
            throw new ArgumentException("Описание не должно превышать 2000 символов.");
    }
}
