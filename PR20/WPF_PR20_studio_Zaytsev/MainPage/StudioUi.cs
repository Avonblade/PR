using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal static class StudioUi
{
    public static bool? ShowOwned(this Window window,Window owner)
    {
        window.Owner = owner;
        return window.ShowDialog();
    }

    public static SearchChoice<Client> ClientChoice(IReadOnlyList<Client> clients,Client? selected)
        => new(clients,(client,text) => client.Matches(text),client => client.Caption,selected);

    public static int NonnegativeInt(TextBox box,string label) =>
        int.TryParse(box.Text.Trim(),NumberStyles.None,StudioText.Russian,out int value) && value >= 0
        ? value : throw new StudioRuleException($"«{label}» — целое число от нуля.");

    public static decimal Amount(TextBox box,string label,decimal max,bool allowZero = false)
    {
        string text = box.Text.Trim().Replace('.',',');
        int separator = text.IndexOf(',');
        if (decimal.TryParse(text,NumberStyles.AllowDecimalPoint,StudioText.Russian,out decimal value) &&
            value >= 0 && (allowZero || value > 0) && value <= max && (separator < 0 || text.Length - separator <= 3))
            return value;
        throw new StudioRuleException($"Проверьте «{label}». Допустимо не более двух знаков после запятой.");
    }

    public static string Numeric(decimal value) => value.ToString("0.##",StudioText.Russian);

    public static TextBox Memo(string? value,int max)
    {
        var box = Input(value,max);
        box.TextWrapping = TextWrapping.Wrap;
        box.AcceptsReturn = true;
        box.MinHeight = 70;
        box.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        return box;
    }

    public static void ShowPhoto(Window owner,PhotoFile photo)
    {
        var window = new Window
        {
            Title = photo.Name, Width = 820, Height = 650, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false, Content = new Image { Source = Decode(photo.Bytes,1100), Margin = new Thickness(12) }
        };
        window.ShowOwned(owner);
    }

    internal static BitmapImage Decode(byte[] bytes,int size)
    {
        using var stream = new MemoryStream(bytes);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.DecodePixelWidth = size;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public static string? ChooseLocation(Window owner)
    {
        var dialog = new OpenFileDialog { Title = "Выберите файл", CheckFileExists = true };
        return dialog.ShowDialog(owner) == true ? dialog.FileName : null;
    }
}

internal sealed class PhotoPicker : StackPanel
{
    public PhotoFile? File { get; private set; }
    public PhotoFile RequiredPhoto() => File ?? throw new StudioRuleException("Выберите фото JPEG или PNG.");

    public PhotoPicker(Window owner)
    {
        var caption = Note("JPEG или PNG, до 10 МБ.");
        var button = Button("Выбрать фото…",async (_,_) =>
        {
            var dialog = new OpenFileDialog { Title = "Выберите фотографию", Filter = "Фотографии (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png" };
            if (dialog.ShowDialog(owner) != true) return;
            try
            {
                var file = new FileInfo(dialog.FileName);
                if (file.Length is 0 or > 10_485_760)
                    throw new StudioRuleException("Допустимый размер фотографии — до 10 МБ.");
                byte[] bytes = await System.IO.File.ReadAllBytesAsync(file.FullName);
                bool jpeg = bytes.Length > 3 && bytes[0] == 0xff && bytes[1] == 0xd8 && bytes[2] == 0xff;
                bool png = bytes.AsSpan().StartsWith(new byte[] { 137,80,78,71,13,10,26,10 });
                if (!jpeg && !png) throw new StudioRuleException("Допустимы файлы JPEG и PNG.");
                _ = StudioUi.Decode(bytes,420);
                File = new PhotoFile(file.Name,bytes);
                caption.Text = file.Name;
            }
            catch (Exception exception)
            {
                UiMessages.ShowError(owner,exception);
            }
        },"SecondaryButton");
        button.HorizontalAlignment = HorizontalAlignment.Left;
        Children.Add(button);
        Children.Add(caption);
    }
}

internal sealed class TableWindow<T> : ClubWindow where T : class
{
    private readonly Func<Task<List<T>>> load;
    private readonly TextBlock count = Note("");
    private readonly WrapPanel tools = new();
    private readonly List<(Button Button,bool RequiresRow)> actions = [];
    private readonly DataGrid grid;
    private readonly List<T> rows = [];
    private Predicate<T>? filter;

    public TableWindow(string title,string label,string subtitle,DataGrid table,Func<Task<List<T>>> load,double width = 950)
        : base(title,label,subtitle,width,620)
    {
        this.load = load;
        grid = table;
        AddDocked(tools,Dock.Top);
        var footer = new StackPanel();
        footer.Children.Add(count);
        footer.Children.Add(ErrorText);
        AddDocked(footer,Dock.Bottom);
        Layout.Children.Add(grid);
        grid.SelectionChanged += (_,_) => UpdateActions();
        Loaded += async (_,_) => await Run(ReloadAsync);
    }

    public T? Selection => grid.SelectedItem as T;

    public void AddAction(string label,Func<T?,Task> action,bool needsRow = true,string? style = "SecondaryButton")
    {
        var button = Button(label,async (_,_) => await Run(() => action(Selection)),style);
        button.Margin = new Thickness(0,0,7,6);
        tools.Children.Add(button);
        actions.Add((button,needsRow));
        UpdateActions();
    }

    public void AddFilters(FrameworkElement controls) => tools.Children.Add(controls);

    public void Filter(Predicate<T> predicate)
    {
        filter = predicate;
        if (grid.ItemsSource is not null) ApplyFilter();
    }

    public async Task ReloadAsync()
    {
        rows.Clear();
        rows.AddRange(await load());
        grid.ItemsSource = rows.ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(grid.ItemsSource);
        view.Filter = item => item is T row && (filter is null || filter(row));
        count.Text = view.IsEmpty ? "Записей по выбранным условиям нет." : $"Показано {grid.Items.Count} из {rows.Count}";
    }

    private void UpdateActions()
    {
        foreach (var action in actions) action.Button.IsEnabled = !action.RequiresRow || Selection is not null;
    }

    private async Task Run(Func<Task> work)
    {
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        try { await work(); }
        catch (Exception exception) { ErrorText.Text = UiMessages.ErrorText(exception); }
        finally { Layout.IsEnabled = true; }
    }
}
