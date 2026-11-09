using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPF_PR18_insurance_Zaytsev.ApplicationData;

namespace WPF_PR18_insurance_Zaytsev.MainPage;

public partial class WorkspaceWindow : Window
{
    private static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    private readonly InsuranceRepository repository;
    private readonly StaffSession session;
    private Entity? current;
    private List<RecordRow> rows = [];
    private ICollectionView? view;

    internal bool SignedOut { get; private set; }

    internal WorkspaceWindow(InsuranceRepository repository)
    {
        this.repository = repository;
        session = repository.Session ?? throw new InvalidOperationException("Войдите в систему.");
        InitializeComponent();
        UserText.Text = session.Name;
        RoleText.Text = session.RoleName;
        Navigation.ItemsSource = InsuranceModel.Sections.Where(section => section.VisibleTo(session.Role)).ToArray();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e) => Navigation.SelectedIndex = 0;

    private async void Navigation_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (Navigation.SelectedItem is not Entity section) return;
        current = section;
        SectionTitle.Text = section.Title;
        SectionDescription.Text = section.Description;
        SearchBox.Clear();
        ConfigureColumns(section);
        UpdateActions();
        await ReloadAsync();
    }

    private void ConfigureColumns(Entity entity)
    {
        RecordsGrid.Columns.Clear();
        foreach (var field in entity.Fields)
            AddColumn(field.Label, field.Kind);
        if (entity.Summary is not null)
        {
            foreach (var column in entity.Summary)
                AddColumn(column.Label, FieldKind.Money);
        }
    }

    private void AddColumn(string header, FieldKind kind)
    {
        string path = $"Values[{RecordsGrid.Columns.Count}]";
        var binding = new Binding(path) { ConverterCulture = Russian, TargetNullValue = "—" };
        binding.StringFormat = kind switch
        {
            FieldKind.Date => "dd.MM.yyyy",
            FieldKind.Money => "N2",
            _ => null
        };
        bool wide = kind is FieldKind.Text or FieldKind.Lookup or FieldKind.Choice;
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
        style.Setters.Add(new Setter(ToolTipProperty, new Binding("Text") { RelativeSource = RelativeSource.Self }));
        RecordsGrid.Columns.Add(new DataGridTextColumn
        {
            Header = header,
            Binding = binding,
            SortMemberPath = path,
            MinWidth = wide ? 120 : 105,
            Width = wide ? new DataGridLength(1, DataGridLengthUnitType.Star) : DataGridLength.SizeToHeader,
            ElementStyle = style
        });
    }

    private async Task ReloadAsync()
    {
        if (current is not Entity section) return;
        Workspace.IsEnabled = false;
        StatusText.Text = "Загрузка…";
        try
        {
            var sort = view?.SortDescriptions.ToArray() ?? [];
            bool restoreSort = Equals(RecordsGrid.Tag, section.Table);
            rows = await repository.LoadAsync(section);
            view = CollectionViewSource.GetDefaultView(rows);
            if (restoreSort)
            {
                foreach (var description in sort)
                    view.SortDescriptions.Add(description);
            }
            RecordsGrid.ItemsSource = view;
            RecordsGrid.Tag = section.Table;
            foreach (var column in RecordsGrid.Columns)
            {
                column.SortDirection = view.SortDescriptions
                    .Where(description => description.PropertyName == column.SortMemberPath)
                    .Select(description => (ListSortDirection?)description.Direction).FirstOrDefault();
            }
            ApplySearch();
            StatusText.Text = $"Обновлено в {DateTime.Now:HH:mm}";
        }
        catch (Exception exception)
        {
            rows = [];
            view = null;
            RecordsGrid.ItemsSource = null;
            CountText.Text = "";
            EmptyText.Text = "Данные раздела недоступны.";
            EmptyText.Visibility = Visibility.Visible;
            StatusText.Text = "Ошибка загрузки";
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            Workspace.IsEnabled = true;
            UpdateActions();
        }
    }

    private void Search_Changed(object sender, TextChangedEventArgs e)
    {
        if (SearchPrompt is null) return;
        SearchPrompt.Visibility = SearchBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;
        ApplySearch();
    }

    private void ApplySearch()
    {
        if (view is null) return;
        string search = SearchBox.Text.Trim();
        view.Filter = search.Length == 0 ? null : item =>
            item is RecordRow row && row.Values.Any(value => DisplayText(value).Contains(search, StringComparison.CurrentCultureIgnoreCase));

        CountText.Text = $"Показано {RecordsGrid.Items.Count} из {rows.Count}";
        EmptyText.Text = rows.Count == 0 ? "В этом разделе пока нет записей." : "По запросу ничего не найдено.";
        EmptyText.Visibility = view.IsEmpty ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string DisplayText(object? value) => value switch
    {
        DateTime date => date.ToString("dd.MM.yyyy", Russian),
        decimal amount => amount.ToString("N2", Russian),
        _ => Convert.ToString(value, Russian) ?? ""
    };

    private void Row_Changed(object sender, SelectionChangedEventArgs e) => UpdateActions();

    private void UpdateActions()
    {
        bool canEdit = current?.EditableBy(session.Role) == true;
        bool hasSelection = RecordsGrid.SelectedItem is RecordRow;
        AddButton.Visibility = EditButton.Visibility = canEdit ? Visibility.Visible : Visibility.Collapsed;
        DeleteButton.Visibility = session.Role == StaffRole.Admin ? Visibility.Visible : Visibility.Collapsed;
        ReadOnlyText.Visibility = canEdit ? Visibility.Collapsed : Visibility.Visible;
        AddButton.IsEnabled = canEdit && view is not null;
        EditButton.IsEnabled = canEdit && hasSelection;
        DeleteButton.IsEnabled = session.Role == StaffRole.Admin && hasSelection;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ReloadAsync();
    private async void Add_Click(object sender, RoutedEventArgs e) => await OpenEditorAsync(null);

    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (RecordsGrid.SelectedItem is RecordRow row)
            await OpenEditorAsync(row);
    }

    private async Task OpenEditorAsync(RecordRow? row)
    {
        if (current is not Entity section || !section.EditableBy(session.Role)) return;
        bool saved = false;
        Workspace.IsEnabled = false;
        try
        {
            var data = await repository.LoadEditorAsync(section, row);
            saved = new RecordWindow(repository, section, row, data) { Owner = this }.ShowDialog() == true;
        }
        catch (Exception exception)
        {
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            Workspace.IsEnabled = true;
        }

        if (saved) await ReloadAsync();
    }

    private async void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (session.Role != StaffRole.Admin || current is not Entity section || RecordsGrid.SelectedItem is not RecordRow row)
            return;
        if (MessageBox.Show(this, $"Удалить выбранную запись из раздела «{section.Title}»?", "Удаление записи",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

        Workspace.IsEnabled = false;
        try
        {
            await repository.DeleteAsync(section, row);
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

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        SignedOut = true;
        Close();
    }
}
