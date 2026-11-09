using System.ComponentModel;
using System.Globalization;
using System.Windows;
using WPF_PR17_restaurant_Zaytsev.ApplicationData;

namespace WPF_PR17_restaurant_Zaytsev.MainPage;

public partial class DishWindow : Window
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private readonly MenuRepository repository;
    private readonly Dish? original;
    private bool busy;

    internal int SavedId { get; private set; }

    internal DishWindow(MenuRepository repository, MenuData menu, Dish? dish)
    {
        this.repository = repository;
        original = dish;
        InitializeComponent();
        Title = dish is null ? "Добавление блюда" : "Редактирование блюда";
        CategoryBox.ItemsSource = menu.Categories;
        CuisineBox.ItemsSource = menu.Cuisines;
        SizeBox.ItemsSource = menu.ServingSizes;
        if (dish is null) return;

        NameBox.Text = dish.Name;
        CategoryBox.SelectedValue = dish.Category.Id;
        CuisineBox.SelectedValue = dish.Cuisine.Id;
        SizeBox.SelectedValue = dish.ServingSize.Id;
        PriceBox.Text = dish.Price.ToString("0.00", Russian);
        DescriptionBox.Text = dish.Description;
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        string name = NameBox.Text.Trim();
        if (name.Length == 0)
        {
            ErrorText.Text = "Введите название блюда.";
            NameBox.Focus();
            return;
        }

        if (CategoryBox.SelectedValue is not int category || CuisineBox.SelectedValue is not int cuisine ||
            SizeBox.SelectedValue is not int size)
        {
            ErrorText.Text = "Выберите категорию, кухню и размер порции.";
            return;
        }

        string priceText = PriceBox.Text.Trim().Replace('.', ',');
        int separator = priceText.IndexOf(',');
        if (!decimal.TryParse(priceText, NumberStyles.AllowDecimalPoint, Russian, out decimal price) ||
            price is <= 0 or > 9_999_999.99m || (separator >= 0 && priceText.Length - separator - 1 > 2))
        {
            ErrorText.Text = "Укажите цену от 0,01 до 9 999 999,99 ₽, не более двух знаков после запятой.";
            PriceBox.Focus();
            return;
        }

        busy = true;
        Form.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            var input = new DishInput(name, category, cuisine, size, price, DescriptionBox.Text.Trim());
            SavedId = await repository.SaveAsync(input, original);
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            busy = false;
            Form.IsEnabled = true;
        }

        if (SavedId > 0)
            DialogResult = true;
    }

    private void Window_Closing(object? sender, CancelEventArgs e) => e.Cancel = busy;
}
