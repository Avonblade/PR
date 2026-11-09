using System.Windows;
using System.Windows.Controls;
using WPF_PR17_restaurant_Zaytsev.ApplicationData;

namespace WPF_PR17_restaurant_Zaytsev.MainPage;

public partial class MenuWindow : Window
{
    private readonly MenuRepository repository = new();
    private MenuData? menu;
    private bool updatingFilters;

    public MenuWindow() => InitializeComponent();

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ReloadAsync();
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ReloadAsync();

    private async Task ReloadAsync(int? selectedId = null)
    {
        Workspace.IsEnabled = false;
        try
        {
            menu = await repository.LoadAsync();
            updatingFilters = true;
            SetFilter(CategoryFilter, menu.Categories, "Все категории");
            SetFilter(CuisineFilter, menu.Cuisines, "Все кухни");
            SetFilter(SizeFilter, menu.ServingSizes, "Все порции");
            updatingFilters = false;
            ApplyFilters(selectedId);
            StatusText.Text = $"Обновлено в {DateTime.Now:HH:mm}";
        }
        catch (Exception exception)
        {
            StatusText.Text = "Не удалось обновить меню. Проверьте подключение к базе.";
            if (menu is null)
                CountText.Text = "Меню недоступно";
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            Workspace.IsEnabled = true;
            UpdateActions();
        }
    }

    private static void SetFilter(ComboBox box, List<LookupItem> choices, string allLabel)
    {
        int previous = box.SelectedValue is int id ? id : 0;
        box.ItemsSource = new[] { new LookupItem(0, allLabel) }.Concat(choices);
        box.SelectedValue = choices.Any(item => item.Id == previous) ? previous : 0;
    }

    private void ApplyFilters(int? selectedId = null)
    {
        if (menu is null || updatingFilters) return;
        selectedId ??= (DishesGrid.SelectedItem as Dish)?.Id;
        string search = SearchBox.Text.Trim();
        var filtered = menu.Dishes.Where(dish =>
            Matches(CategoryFilter, dish.Category) &&
            Matches(CuisineFilter, dish.Cuisine) &&
            Matches(SizeFilter, dish.ServingSize) &&
            (dish.Name.Contains(search, StringComparison.CurrentCultureIgnoreCase) ||
             dish.Description.Contains(search, StringComparison.CurrentCultureIgnoreCase))).ToList();

        DishesGrid.ItemsSource = filtered;
        DishesGrid.SelectedItem = filtered.FirstOrDefault(dish => dish.Id == selectedId);
        CountText.Text = $"Показано {filtered.Count} из {menu.Dishes.Count}";
        EmptyText.Text = menu.Dishes.Count == 0 ? "В меню пока нет блюд." : "По заданным условиям ничего не найдено.";
        EmptyText.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private static bool Matches(ComboBox filter, LookupItem value)
        => filter.SelectedValue is not int id || id == 0 || id == value.Id;

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (menu is null) return;
        updatingFilters = true;
        SearchBox.Clear();
        CategoryFilter.SelectedValue = 0;
        CuisineFilter.SelectedValue = 0;
        SizeFilter.SelectedValue = 0;
        updatingFilters = false;
        ApplyFilters();
    }

    private void Session_Click(object sender, RoutedEventArgs e)
    {
        if (repository.EditorName is not null)
            repository.SignOut();
        else
            new LoginWindow(repository) { Owner = this }.ShowDialog();

        SessionButton.Content = repository.EditorName is null ? "Войти" : "Выйти";
        SessionText.Text = repository.EditorName ?? "Гостевой режим";
        UpdateActions();
    }

    private void Dish_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (DishesGrid.SelectedItem is Dish dish)
            DescriptionText.Text = dish.Description.Length == 0 ? "Описание не указано." : dish.Description;
        else
            DescriptionText.Text = "Выберите блюдо в меню.";
        UpdateActions();
    }

    private void UpdateActions()
    {
        bool canEdit = menu is not null && repository.EditorName is not null;
        AddButton.IsEnabled = canEdit;
        EditButton.IsEnabled = canEdit && DishesGrid.SelectedItem is Dish;
        DeleteButton.IsEnabled = EditButton.IsEnabled;
    }

    private async void Add_Click(object sender, RoutedEventArgs e) => await OpenEditorAsync(null);

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (DishesGrid.SelectedItem is Dish dish)
            await OpenEditorAsync(dish);
    }

    private async Task OpenEditorAsync(Dish? dish)
    {
        if (menu is null || repository.EditorName is null) return;
        var editor = new DishWindow(repository, menu, dish) { Owner = this };
        if (editor.ShowDialog() == true)
            await ReloadAsync(editor.SavedId);
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (repository.EditorName is null || DishesGrid.SelectedItem is not Dish dish) return;
        if (MessageBox.Show(this, $"Удалить блюдо «{dish.Name}»?", "Удаление блюда",
                MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes)
            return;

        Workspace.IsEnabled = false;
        try
        {
            await repository.DeleteAsync(dish);
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            Workspace.IsEnabled = true;
        }
        await ReloadAsync();
    }
}
