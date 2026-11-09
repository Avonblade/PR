using System.Globalization;

namespace WPF_PR17_restaurant_Zaytsev.ApplicationData;

internal sealed record LookupItem(int Id, string Name);

internal sealed record Dish(int Id, string Name, LookupItem Category, LookupItem Cuisine,
    LookupItem ServingSize, decimal Price, string Description, byte[] Version)
{
    public string PriceText => Price.ToString("N2", CultureInfo.GetCultureInfo("ru-RU")) + " ₽";
}

internal sealed record MenuData(List<Dish> Dishes, List<LookupItem> Categories,
    List<LookupItem> Cuisines, List<LookupItem> ServingSizes);

internal sealed record DishInput(string Name, int CategoryId, int CuisineId,
    int ServingSizeId, decimal Price, string Description);
