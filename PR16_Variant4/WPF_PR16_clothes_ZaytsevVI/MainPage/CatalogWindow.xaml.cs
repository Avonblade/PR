using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WPF_PR16_clothes_ZaytsevVI.ApplicationData;

namespace WPF_PR16_clothes_ZaytsevVI.MainPage;

public partial class CatalogWindow : Window
{
    private readonly ClothingRepository repository = new();
    private List<ClothingProduct> products = [];
    private List<LookupItem> categories = [];
    private List<LookupItem> materials = [];
    private List<LookupItem> sizes = [];
    private List<LookupItem> colors = [];
    private bool ready;
    private bool busy;

    public CatalogWindow() => InitializeComponent();

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync(int? selectedId = null)
    {
        if (busy) return;
        busy = true;
        Workspace.IsEnabled = false;
        StatusText.Text = "Загрузка данных из SQL Server…";
        try
        {
            var productsTask = repository.GetProductsAsync();
            var categoriesTask = repository.GetCategoriesAsync();
            var materialsTask = repository.GetMaterialsAsync();
            var sizesTask = repository.GetSizesAsync();
            var colorsTask = repository.GetColorsAsync();
            await Task.WhenAll(productsTask, categoriesTask, materialsTask, sizesTask, colorsTask);
            int categoryId = CategoryFilter.SelectedValue as int? ?? 0;
            int materialId = MaterialFilter.SelectedValue as int? ?? 0;
            int sizeId = SizeFilter.SelectedValue as int? ?? 0;
            int colorId = ColorFilter.SelectedValue as int? ?? 0;
            ready = false;
            products = productsTask.Result;
            categories = categoriesTask.Result;
            materials = materialsTask.Result;
            sizes = sizesTask.Result;
            colors = colorsTask.Result;
            CategoryFilter.ItemsSource = new[] { new LookupItem(0, "Все категории") }.Concat(categories).ToList();
            MaterialFilter.ItemsSource = new[] { new LookupItem(0, "Все материалы") }.Concat(materials).ToList();
            SizeFilter.ItemsSource = new[] { new LookupItem(0, "Все") }.Concat(sizes).ToList();
            ColorFilter.ItemsSource = new[] { new LookupItem(0, "Все цвета") }.Concat(colors).ToList();
            SizeFilter.SelectedValue = sizes.Any(s => s.Id == sizeId) ? sizeId : 0;
            ColorFilter.SelectedValue = colors.Any(c => c.Id == colorId) ? colorId : 0;
            CategoryFilter.SelectedValue = categories.Any(c => c.Id == categoryId) ? categoryId : 0;
            MaterialFilter.SelectedValue = materials.Any(m => m.Id == materialId) ? materialId : 0;
            TotalProductsText.Text = products.Count.ToString();
            TotalCategoriesText.Text = categories.Count.ToString();
            TotalStockText.Text = products.Sum(p => (long)p.Stock).ToString("N0", CultureInfo.GetCultureInfo("ru-RU"));
            ready = true;
            ApplyFilters(selectedId);
            StatusText.Text = $"Обновлено в {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception exception)
        {
            StatusText.Text = "База недоступна — проверьте подключение и нажмите «Обновить».";
            CounterText.Text = ready ? CounterText.Text : "Нет подключения";
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            busy = false;
            Workspace.IsEnabled = true;
        }
    }

    private void ApplyFilters(int? selectedId = null)
    {
        if (!ready) return;
        selectedId ??= (ProductsGrid.SelectedItem as ClothingProduct)?.Id;
        string search = SearchBox.Text.Trim();
        int categoryId = CategoryFilter.SelectedValue as int? ?? 0;
        int materialId = MaterialFilter.SelectedValue as int? ?? 0;
        int sizeId = SizeFilter.SelectedValue as int? ?? 0;
        int colorId = ColorFilter.SelectedValue as int? ?? 0;
        IEnumerable<ClothingProduct> filtered = products.Where(p =>
            (categoryId == 0 || p.CategoryId == categoryId) &&
            (materialId == 0 || p.MaterialId == materialId) &&
            (sizeId == 0 || p.SizeId == sizeId) &&
            (colorId == 0 || p.ColorId == colorId) &&
            (search.Length == 0 || p.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
             p.Article.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
             p.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase)));
        filtered = SortBox.SelectedIndex switch
        {
            1 => filtered.OrderBy(p => p.Price).ThenBy(p => p.Name),
            2 => filtered.OrderByDescending(p => p.Price).ThenBy(p => p.Name),
            3 => filtered.OrderBy(p => p.Name),
            _ => filtered.OrderByDescending(p => p.Id)
        };
        var visible = filtered.ToList();
        ProductsGrid.ItemsSource = visible;
        ProductsGrid.SelectedItem = visible.FirstOrDefault(p => p.Id == selectedId);
        CounterText.Text = $"Показано {visible.Count} из {products.Count}";
        EmptyText.Text = products.Count == 0
            ? "В каталоге пока нет товаров. Добавьте первый товар."
            : "По заданным условиям ничего не найдено. Сбросьте фильтры.";
        EmptyText.Visibility = visible.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Filter_Changed(object sender, TextChangedEventArgs e) => ApplyFilters();
    private void SelectionFilter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (!ready) return;
        ready = false;
        SearchBox.Clear();
        CategoryFilter.SelectedValue = 0;
        MaterialFilter.SelectedValue = 0;
        SizeFilter.SelectedValue = 0;
        ColorFilter.SelectedValue = 0;
        SortBox.SelectedIndex = 0;
        ready = true;
        ApplyFilters();
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ReloadAsync();

    private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selected = ProductsGrid.SelectedItem as ClothingProduct;
        EditButton.IsEnabled = selected is not null;
        DeleteButton.IsEnabled = selected is not null;
        DescriptionText.Text = selected is null
            ? "Выберите товар, чтобы увидеть описание. Двойной щелчок открывает редактирование."
            : string.IsNullOrWhiteSpace(selected.Description) ? "Описание не указано." : selected.Description;
        DescriptionText.ToolTip = selected?.Description;
    }

    private async void Add_Click(object sender, RoutedEventArgs e) => await OpenEditorAsync(null);
    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (ProductsGrid.SelectedItem is ClothingProduct product)
            await OpenEditorAsync(product);
    }

    private async void ProductsGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source ||
            ItemsControl.ContainerFromElement(ProductsGrid, source) is not DataGridRow)
            return;
        if (ProductsGrid.SelectedItem is ClothingProduct product)
            await OpenEditorAsync(product);
    }

    private async Task OpenEditorAsync(ClothingProduct? product)
    {
        if (!ready || busy) return;
        var editor = new ProductWindow(repository, categories, materials, sizes, colors, product) { Owner = this };
        if (editor.ShowDialog() == true)
            await ReloadAsync(editor.SavedProductId);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (busy || ProductsGrid.SelectedItem is not ClothingProduct product) return;
        if (MessageBox.Show(this, $"Удалить товар «{product.Name}» ({product.Article})?\nЭто действие нельзя отменить.",
                "Удаление товара", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;
        busy = true;
        Workspace.IsEnabled = false;
        try
        {
            await repository.DeleteAsync(product);
            StatusText.Text = "Товар удалён.";
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            busy = false;
            Workspace.IsEnabled = true;
        }
        await ReloadAsync();
    }
}
