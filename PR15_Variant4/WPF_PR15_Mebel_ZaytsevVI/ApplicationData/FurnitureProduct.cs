using System.Globalization;

namespace WPF_PR15_Mebel_ZaytsevVI.ApplicationData;

public sealed record LookupItem(int Id, string Name);

public sealed class FurnitureProduct
{
    public int Id { get; set; }
    public string Article { get; set; } = "";
    public string Name { get; set; } = "";
    public int CategoryId { get; set; }
    public int MaterialId { get; set; }
    public string CategoryName { get; set; } = "";
    public string MaterialName { get; set; } = "";
    public decimal Price { get; set; }
    public int WidthMm { get; set; } = 600;
    public int DepthMm { get; set; } = 600;
    public int HeightMm { get; set; } = 820;
    public int Stock { get; set; }
    public string Description { get; set; } = "";
    public byte[] Version { get; set; } = [];

    public string PriceText => Price.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
    public string DimensionsText => $"{WidthMm} × {DepthMm} × {HeightMm}";
    public string StockText => Stock == 0 ? "Нет в наличии" : $"{Stock} шт.";
    public bool IsAvailable => Stock > 0;
}
