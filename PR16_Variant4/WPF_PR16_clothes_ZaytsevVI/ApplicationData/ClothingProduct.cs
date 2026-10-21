using System.Globalization;

namespace WPF_PR16_clothes_ZaytsevVI.ApplicationData;

public sealed record LookupItem(int Id, string Name);

public sealed class ClothingProduct
{
    public int Id { get; set; }
    public string Article { get; set; } = "";
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public int MaterialId { get; set; }
    public int SizeId { get; set; }
    public int ColorId { get; set; }
    public string CategoryName { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public string SizeName { get; set; } = "";
    public string ColorName { get; set; } = "";
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public string Description { get; set; } = "";
    public byte[] Version { get; set; } = [];

    public string PriceText => Price.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
    public string StockText => Stock == 0 ? "Нет в наличии" : $"{Stock} шт.";
    public bool IsAvailable => Stock > 0;
}
