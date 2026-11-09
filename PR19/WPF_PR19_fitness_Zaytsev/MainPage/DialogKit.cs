using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WPF_PR19_fitness_Zaytsev.ApplicationData;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal sealed record Option<T>(T Value, string Text);

internal class ClubWindow : Window
{
    protected readonly DockPanel Layout = new();
    protected readonly TextBlock ErrorText = new()
    {
        TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.FromRgb(161, 38, 61))
    };

    protected ClubWindow(string title, string label, string subtitle, double width = 710, double? height = null)
    {
        Title = title;
        Width = width;
        MaxHeight = SystemParameters.WorkArea.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        Language = System.Windows.Markup.XmlLanguage.GetLanguage("ru-RU");
        SetResourceReference(StyleProperty, typeof(Window));
        if (height is double fixedHeight)
            Height = Math.Min(fixedHeight, MaxHeight);
        else
        {
            SizeToContent = SizeToContent.Height;
            ResizeMode = ResizeMode.NoResize;
        }

        Layout.Margin = new Thickness(22);
        Content = Layout;
        var heading = new StackPanel();
        heading.Children.Add(new TextBlock { Text = label, FontSize = 11, FontWeight = FontWeights.SemiBold });
        heading.Children.Add(new TextBlock { Text = title, FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 3, 0, 3) });
        heading.Children.Add(new TextBlock { Text = subtitle, TextWrapping = TextWrapping.Wrap });
        var banner = new Border { Padding = new Thickness(14, 10, 14, 11), CornerRadius = new CornerRadius(8),
            Margin = new Thickness(0, 0, 0, 12), Child = heading };
        banner.SetResourceReference(BackgroundProperty, "BrandLight");
        DockPanel.SetDock(banner, Dock.Top);
        Layout.Children.Add(banner);
    }

    protected void AddDocked(UIElement element, Dock dock)
    {
        DockPanel.SetDock(element, dock);
        Layout.Children.Add(element);
    }
}

internal sealed class FormWindow : ClubWindow
{
    private readonly StackPanel fields = new();
    private readonly StackPanel footer = new() { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
    private Func<Task>? submit;
    private bool saving;

    public FormWindow(string title, string label, string subtitle, string action, double width = 610)
        : base(title, label, subtitle, width)
    {
        var buttons = new StackPanel();
        ErrorText.MinHeight = 21;
        ErrorText.Margin = new Thickness(0, 2, 0, 8);
        AutomationProperties.SetAutomationId(ErrorText, "ValidationText");
        buttons.Children.Add(ErrorText);
        buttons.Children.Add(footer);
        var cancel = DialogKit.Button("Отмена", style: "SecondaryButton");
        cancel.IsCancel = true;
        footer.Children.Add(cancel);
        var save = DialogKit.Button(action, async (_, _) => await SubmitAsync());
        save.IsDefault = true;
        AutomationProperties.SetAutomationId(save, "SubmitButton");
        footer.Children.Add(save);
        AddDocked(buttons, Dock.Bottom);
        Layout.Children.Add(new ScrollViewer
        {
            Content = fields, VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        });
        Closing += (_, args) => args.Cancel = saving;
    }

    public void Add(string label, FrameworkElement input) => fields.Children.Add(DialogKit.Field(label, input));
    public void Add(UIElement input) => fields.Children.Add(input);
    public void OnSubmit(Func<Task> action) => submit = action;

    private async Task SubmitAsync()
    {
        if (saving || submit is null) return;
        saving = true;
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        bool success = false;
        try
        {
            await submit();
            success = true;
        }
        catch (Exception exception)
        {
            ErrorText.Text = UiMessages.ErrorText(exception);
        }
        finally
        {
            Layout.IsEnabled = true;
            saving = false;
        }

        if (success) DialogResult = true;
    }
}

internal sealed class SearchChoice<T> : StackPanel
{
    private readonly IReadOnlyList<T> source;
    private readonly Func<T, string, bool> matches;
    private readonly Func<T, string> caption;
    private readonly ComboBox choices;
    public TextBox Search { get; }
    public T? Value => (choices.SelectedItem as Option<T>) is { } option ? option.Value : default;
    public event SelectionChangedEventHandler? SelectionChanged;

    public SearchChoice(IReadOnlyList<T> source, Func<T, string, bool> matches, Func<T, string> caption, T? selected)
    {
        this.source = source;
        this.matches = matches;
        this.caption = caption;
        Search = DialogKit.Input("", 100);
        Search.ToolTip = "Введите часть имени, телефона или номера карты";
        AutomationProperties.SetName(Search, "Поиск клиента");
        choices = DialogKit.Choices(source, caption);
        AutomationProperties.SetName(choices, "Найденные клиенты");
        choices.Margin = new Thickness(0, 5, 0, 0);
        if (selected is not null) Select(selected);
        choices.SelectionChanged += (sender, args) => SelectionChanged?.Invoke(sender, args);
        Search.TextChanged += (_, _) => Filter();
        Children.Add(Search);
        Children.Add(choices);
    }

    private void Select(T value) => choices.SelectedItem = choices.Items.Cast<Option<T>>()
        .FirstOrDefault(item => EqualityComparer<T>.Default.Equals(item.Value, value));

    private void Filter()
    {
        var previous = Value;
        string search = Search.Text.Trim();
        var items = source.Where(item => matches(item, search)).Select(item => new Option<T>(item, caption(item))).ToList();
        choices.ItemsSource = items;
        if (previous is not null) Select(previous);
        if (choices.SelectedIndex < 0 && items.Count == 1) choices.SelectedIndex = 0;
    }
}

internal static class DialogKit
{
    private static readonly CultureInfo Culture = FitnessText.Russian;

    public static Button Button(string text, RoutedEventHandler? click = null, string? style = null)
    {
        var button = new Button { Content = text, Margin = new Thickness(0, 0, 8, 0) };
        if (style is not null) button.SetResourceReference(FrameworkElement.StyleProperty, style);
        if (click is not null) button.Click += click;
        return button;
    }

    public static TextBox Input(string? value = null, int max = 100, bool readOnly = false)
        => new() { Text = value ?? "", MaxLength = max, IsReadOnly = readOnly };

    public static TextBlock Note(string value) => new()
    {
        Text = value, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 4, 0, 8)
    };

    public static FrameworkElement Field(string label, FrameworkElement input)
    {
        var panel = new StackPanel { Margin = new Thickness(0, 0, 0, 8) };
        panel.Children.Add(new Label { Content = label, Target = input as Control });
        panel.Children.Add(input);
        return panel;
    }

    public static Grid Pair(FrameworkElement left, FrameworkElement right)
    {
        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        left.Margin = new Thickness(0, 0, 8, 8);
        right.Margin = new Thickness(8, 0, 0, 8);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);
        return grid;
    }

    public static ComboBox Choices<T>(IEnumerable<T> values, Func<T, string> caption, T? initial = default, bool select = false)
    {
        var options = values.Select(value => new Option<T>(value, caption(value))).ToList();
        return new ComboBox
        {
            ItemsSource = options, DisplayMemberPath = nameof(Option<T>.Text),
            SelectedItem = select ? options.FirstOrDefault(option => EqualityComparer<T>.Default.Equals(option.Value, initial)) : null
        };
    }

    public static ComboBox EnumChoice<T>(Func<T, string> caption, T initial) where T : struct, Enum
        => Choices(Enum.GetValues<T>(), caption, initial, true);

    public static T Selected<T>(ComboBox box, string label) => box.SelectedItem is Option<T> item
        ? item.Value : throw new FitnessRuleException($"Выберите {label}.");

    public static T? Selection<T>(ComboBox box) where T : class
        => (box.SelectedItem as Option<T>)?.Value;

    public static string DateText(DateTime date) => date.ToString("dd.MM.yyyy", Culture);
    public static string StampText(DateTime date) => date.ToString("dd.MM.yyyy HH:mm:ss", Culture);

    public static DateTime Date(TextBox box, string label)
        => DateTime.TryParseExact(box.Text.Trim(), "dd.MM.yyyy", Culture, DateTimeStyles.None, out var date)
            ? date : throw new FitnessRuleException($"Введите {label} в формате ДД.ММ.ГГГГ.");

    public static DateTime Stamp(TextBox box)
        => DateTime.TryParseExact(box.Text.Trim(), ["dd.MM.yyyy HH:mm:ss", "dd.MM.yyyy HH:mm"], Culture,
            DateTimeStyles.None, out var date) ? date :
            throw new FitnessRuleException("Введите время выхода в формате ДД.ММ.ГГГГ ЧЧ:ММ:СС.");

    public static TimeSpan Time(TextBox box)
        => TimeSpan.TryParseExact(box.Text.Trim(), @"hh\:mm", Culture, out var time) && time < TimeSpan.FromDays(1)
            ? time : throw new FitnessRuleException("Введите время в формате ЧЧ:ММ.");

    public static int PositiveInt(TextBox box, string label) =>
        int.TryParse(box.Text.Trim(), NumberStyles.None, Culture, out int number) && number > 0
        ? number : throw new FitnessRuleException($"{label} — целое число больше нуля.");

    public static decimal PositiveAmount(TextBox box)
    {
        string text = box.Text.Trim().Replace('.', ',');
        int point = text.IndexOf(',');
        if (decimal.TryParse(text, NumberStyles.AllowDecimalPoint, Culture, out decimal amount) &&
            amount is > 0 and <= 9_999_999.99m && (point < 0 || text.Length - point <= 3))
            return amount;
        throw new FitnessRuleException("Введите стоимость больше нуля, не более двух знаков после запятой.");
    }

    public static DataGrid Table(params (string Header, string Path, int Width, string? Format)[] columns)
    {
        var grid = new DataGrid { Margin = new Thickness(0, 8, 0, 0) };
        foreach (var (header, path, width, format) in columns)
            grid.Columns.Add(new DataGridTextColumn
            {
                Header = header, SortMemberPath = path,
                Width = width == 0 ? new DataGridLength(1, DataGridLengthUnitType.Star) : new DataGridLength(width),
                MinWidth = width == 0 ? 125 : width,
                Binding = new Binding(path) { StringFormat = format, ConverterCulture = Culture, TargetNullValue = "—" }
            });
        return grid;
    }

    public static SearchChoice<ClientRow> ClientPicker(IReadOnlyList<ClientRow> clients, ClientRow? selected)
        => new(clients, (client, search) => client.Client.Matches(search), client => client.Client.Caption, selected);
}
