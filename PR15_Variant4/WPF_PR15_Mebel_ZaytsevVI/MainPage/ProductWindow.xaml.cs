using System.ComponentModel;
using System.Globalization;
using System.Windows;
using WPF_PR15_Mebel_ZaytsevVI.ApplicationData;

namespace WPF_PR15_Mebel_ZaytsevVI.MainPage;

public partial class ProductWindow : Window
{
    private readonly FurnitureRepository repository;
    private readonly FurnitureProduct? original;
    private readonly string initialState;
    private bool saving;
    public int SavedProductId { get; private set; }

    public ProductWindow(FurnitureRepository repository, IReadOnlyList<LookupItem> categories,
        IReadOnlyList<LookupItem> materials, FurnitureProduct? product = null)
    {
        InitializeComponent();
        this.repository = repository;
        original = product;
        CategoryBox.ItemsSource = categories;
        MaterialBox.ItemsSource = materials;
        if (product is not null)
        {
            Title = "Редактирование товара — мебель для кухни";
            HeadingText.Text = "Редактирование товара";
            IdText.Text = $"Номер товара: {product.Id} · назначается автоматически";
            ArticleBox.Text = product.Article;
            NameBox.Text = product.Name;
            CategoryBox.SelectedValue = product.CategoryId;
            MaterialBox.SelectedValue = product.MaterialId;
            PriceBox.Text = product.Price.ToString("0.00", CultureInfo.GetCultureInfo("ru-RU"));
            StockBox.Text = product.Stock.ToString(CultureInfo.InvariantCulture);
            WidthBox.Text = product.WidthMm.ToString(CultureInfo.InvariantCulture);
            DepthBox.Text = product.DepthMm.ToString(CultureInfo.InvariantCulture);
            HeightBox.Text = product.HeightMm.ToString(CultureInfo.InvariantCulture);
            DescriptionBox.Text = product.Description;
        }
        else
        {
            CategoryBox.SelectedIndex = -1;
            MaterialBox.SelectedIndex = -1;
            PriceBox.Text = "0,00";
            StockBox.Text = "0";
            WidthBox.Text = "600";
            DepthBox.Text = "600";
            HeightBox.Text = "820";
        }
        initialState = FormState();
        Loaded += (_, _) => ArticleBox.Focus();
    }

    private string FormState() => string.Join('\u001f', ArticleBox.Text, NameBox.Text,
        CategoryBox.SelectedValue, MaterialBox.SelectedValue, PriceBox.Text, StockBox.Text,
        WidthBox.Text, DepthBox.Text, HeightBox.Text, DescriptionBox.Text);

    private static int ParseInteger(string text, string label)
    {
        if (!int.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            throw new ArgumentException($"Поле «{label}» должно содержать целое неотрицательное число.");
        return value;
    }

    private FurnitureProduct ReadProduct()
    {
        string text = PriceBox.Text.Trim().Replace(',', '.');
        if (!decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal price))
            throw new ArgumentException("Введите корректную цену: например, 12900,50.");
        return new FurnitureProduct
        {
            Id = original?.Id ?? 0,
            Version = original?.Version ?? [],
            Article = ArticleBox.Text.Trim(), Name = NameBox.Text.Trim(),
            CategoryId = CategoryBox.SelectedValue as int? ?? 0,
            MaterialId = MaterialBox.SelectedValue as int? ?? 0,
            Price = price, Stock = ParseInteger(StockBox.Text, "Количество"),
            WidthMm = ParseInteger(WidthBox.Text, "Ширина"),
            DepthMm = ParseInteger(DepthBox.Text, "Глубина"),
            HeightMm = ParseInteger(HeightBox.Text, "Высота"),
            Description = DescriptionBox.Text.Trim()
        };
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (saving) return;
        try
        {
            var product = ReadProduct();
            FurnitureRepository.Validate(product);
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
