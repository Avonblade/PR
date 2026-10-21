using System.ComponentModel;
using System.Globalization;
using System.Windows;
using WPF_PR16_clothes_ZaytsevVI.ApplicationData;

namespace WPF_PR16_clothes_ZaytsevVI.MainPage;

public partial class ProductWindow : Window
{
    private readonly ClothingRepository repository;
    private readonly ClothingProduct? original;
    private readonly string initialState;
    private bool saving;
    public int SavedProductId { get; private set; }

    public ProductWindow(ClothingRepository repository, IReadOnlyList<LookupItem> categories,
        IReadOnlyList<LookupItem> materials, IReadOnlyList<LookupItem> sizes,
        IReadOnlyList<LookupItem> colors, ClothingProduct? product = null)
    {
        InitializeComponent();
        this.repository = repository;
        original = product;
        CategoryBox.ItemsSource = categories;
        MaterialBox.ItemsSource = materials;
        SizeBox.ItemsSource = sizes;
        ColorBox.ItemsSource = colors;
        if (product is not null)
        {
            Title = "Редактирование товара — одежда Alisa";
            HeadingText.Text = "Редактирование товара";
            IdText.Text = $"Номер товара: {product.Id} · назначается автоматически";
            ArticleBox.Text = product.Article;
            NameBox.Text = product.Name;
            CategoryBox.SelectedValue = product.CategoryId;
            MaterialBox.SelectedValue = product.MaterialId;
            SizeBox.SelectedValue = product.SizeId;
            ColorBox.SelectedValue = product.ColorId;
            PriceBox.Text = product.Price.ToString("0.00", CultureInfo.GetCultureInfo("ru-RU"));
            StockBox.Text = product.Stock.ToString(CultureInfo.InvariantCulture);
            DescriptionBox.Text = product.Description;
        }
        else
        {
            CategoryBox.SelectedIndex = -1;
            MaterialBox.SelectedIndex = -1;
            SizeBox.SelectedIndex = -1;
            ColorBox.SelectedIndex = -1;
            PriceBox.Text = "0,00";
            StockBox.Text = "0";
        }
        initialState = FormState();
        Loaded += (_, _) => ArticleBox.Focus();
    }

    private string FormState() => string.Join('\u001f', ArticleBox.Text, NameBox.Text,
        CategoryBox.SelectedValue, MaterialBox.SelectedValue, PriceBox.Text, StockBox.Text,
        SizeBox.SelectedValue, ColorBox.SelectedValue, DescriptionBox.Text);

    private static int ParseInteger(string text, string label)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            throw new ArgumentException($"Поле «{label}» должно содержать целое неотрицательное число.");
        return value;
    }

    private ClothingProduct ReadProduct()
    {
        string text = PriceBox.Text.Trim().Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price))
            throw new ArgumentException("Введите корректную цену: например, 12900,50.");
        return new ClothingProduct
        {
            Id = original?.Id ?? 0,
            Version = original?.Version ?? [],
            Article = ArticleBox.Text.Trim(), Name = NameBox.Text.Trim(),
            CategoryId = CategoryBox.SelectedValue as int? ?? 0,
            MaterialId = MaterialBox.SelectedValue as int? ?? 0,
            SizeId = SizeBox.SelectedValue as int? ?? 0,
            ColorId = ColorBox.SelectedValue as int? ?? 0,
            Price = price, Stock = ParseInteger(StockBox.Text, "Количество"),
            Description = DescriptionBox.Text.Trim()
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (saving) return;
        try
        {
            var product = ReadProduct();
            ClothingRepository.Validate(product);
            saving = true;
            FormPanel.IsEnabled = false;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SaveStatusText.Text = "Сохранение…";
            SavedProductId = await repository.SaveAsync(product);
            saving = false;
            DialogResult = true;
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
            SaveStatusText.Text = "Проверьте данные и повторите сохранение";
        }
        finally
        {
            saving = false;
            FormPanel.IsEnabled = true;
            SaveButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        if (saving)
        {
            e.Cancel = true;
            return;
        }
        if (DialogResult != true && initialState != FormState())
            e.Cancel = MessageBox.Show(this, "Закрыть форму без сохранения изменений?", "Несохранённые изменения",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes;
    }
}
