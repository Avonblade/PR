using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;

namespace WPF_PR20_studio_Zaytsev.MainPage;

public partial class WorkspaceWindow : Window
{
    private readonly StudioRepository repo;
    private readonly Session session;
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private List<Booking> rows = [];
    private ICollectionView? view;
    private bool busy;
    private bool updatingFilters;
    internal bool SignedOut { get; private set; }

    internal WorkspaceWindow(StudioRepository repo)
    {
        this.repo = repo;
        session = repo.Session!;
        InitializeComponent();
        Person.Text = session.Name;
        Role.Text = session.Role.RoleName();
        Clock.Text = StampText(DateTime.Now);
        timer.Tick += (_,_) => Clock.Text = StampText(DateTime.Now);
        Closed += (_,_) => timer.Stop();
        StateFilter.ItemsSource = new[] { "Все статусы" }.Concat(Enum.GetValues<BookingState>().Select(StudioText.StateName));
        DateFilter.ItemsSource = new[] { "Все даты", "Сегодня", "На этой неделе", "В следующем месяце" };
        PropFilter.ItemsSource = new[] { "Любой", "Свободно", "Занято" };
        StateFilter.SelectedIndex = DateFilter.SelectedIndex = PropFilter.SelectedIndex = 0;
        NewButton.Visibility = MoveButton.Visibility = CancelButton.Visibility = ClientButton.Visibility =
            ReportButton.Visibility = AmountFilters.Visibility = Show(session.Office);
        StaffButton.Visibility = Show(session.Admin);
        TotalColumn.Visibility = AdvanceColumn.Visibility = Show(session.Office);
        UpdateButtons();
    }

    private static Visibility Show(bool show) => show ? Visibility.Visible : Visibility.Collapsed;
    private Booking? SelectedBooking => BookingsGrid.SelectedItem as Booking;
    private async void Window_Loaded(object sender,RoutedEventArgs e)
    {
        timer.Start();
        await RunAsync(ReloadAsync);
    }
    private async void Refresh_Click(object sender,RoutedEventArgs e) => await RunAsync(ReloadAsync);

    private async Task ReloadAsync()
    {
        var bookings = await repo.BookingsAsync();
        int? selected = SelectedBooking?.Id;
        int room = (RoomFilter.SelectedItem as Option<int>)?.Value ?? 0;
        int photo = (PhotoFilter.SelectedItem as Option<int>)?.Value ?? 0;
        var sort = view?.SortDescriptions.ToArray() ?? [new SortDescription(nameof(Booking.Start),ListSortDirection.Ascending)];
        rows = bookings;
        updatingFilters = true;
        try
        {
            Fill(RoomFilter,"Все залы",rows.Select(b => new Option<int>(b.RoomId,b.Room)).DistinctBy(x => x.Value),room);
            Fill(PhotoFilter,"Все фотографы",rows.Select(b => new Option<int>(b.PhotoId,b.Photographer)).DistinctBy(x => x.Value),photo);
        }
        finally { updatingFilters = false; }

        view = CollectionViewSource.GetDefaultView(rows);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(Booking.Room)));
        BookingsGrid.ItemsSource = view;
        foreach (var entry in sort) view.SortDescriptions.Add(entry);
        foreach (var column in BookingsGrid.Columns)
            column.SortDirection = sort.Where(item => item.PropertyName == column.SortMemberPath)
                .Select(item => (ListSortDirection?)item.Direction).FirstOrDefault();
        ApplyFilters();
        BookingsGrid.SelectedItem = rows.FirstOrDefault(b => b.Id == selected && view.Contains(b));
        Status.Text = $"Обновлено в {DateTime.Now:HH:mm}";
    }

    private static void Fill(ComboBox box,string label,IEnumerable<Option<int>> options,int selected)
    {
        var list = new[] { new Option<int>(0,label) }.Concat(options.OrderBy(o => o.Text)).ToList();
        box.ItemsSource = list;
        box.SelectedItem = list.Find(o => o.Value == selected) ?? list[0];
    }

    private void Filter_Changed(object sender,RoutedEventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (view is null || updatingFilters) return;
        bool minValid = ReadAmount(MinBox.Text,out decimal? min);
        bool maxValid = ReadAmount(MaxBox.Text,out decimal? max);
        if (!minValid || !maxValid || min > max)
        {
            FilterError.Text = "Проверьте диапазон сумм.";
            return;
        }
        FilterError.Text = "";
        string term = SearchBox.Text.Trim();
        int state = StateFilter.SelectedIndex - 1;
        int room = (RoomFilter.SelectedItem as Option<int>)?.Value ?? 0;
        int photo = (PhotoFilter.SelectedItem as Option<int>)?.Value ?? 0;
        int stock = PropFilter.SelectedIndex;
        DateTime today = DateTime.Today;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var next = new DateTime(today.Year,today.Month,1).AddMonths(1);
        (DateTime from,DateTime to) = DateFilter.SelectedIndex switch
        {
            1 => (today,today.AddDays(1)), 2 => (monday,monday.AddDays(7)),
            3 => (next,next.AddMonths(1)), _ => (DateTime.MinValue,DateTime.MaxValue)
        };

        view.Filter = item => item is Booking b && b.Matches(term) &&
            (state < 0 || (int)b.State == state) && (room == 0 || b.RoomId == room) &&
            (photo == 0 || b.PhotoId == photo) && b.Start < to && b.End > from &&
            (stock <= 0 || b.PropsFree == (stock == 1)) && (min is null || b.Total >= min) && (max is null || b.Total <= max);
        Counter.Text = $"Показано {BookingsGrid.Items.Count} из {rows.Count}";
        Empty.Visibility = Show(view.IsEmpty);
        UpdateButtons();
    }

    private static bool ReadAmount(string value,out decimal? amount)
    {
        amount = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!decimal.TryParse(value.Trim().Replace('.',','),NumberStyles.AllowDecimalPoint,StudioText.Russian,out var number)
            || number < 0) return false;
        amount = number;
        return true;
    }

    private void Reset_Click(object sender,RoutedEventArgs e)
    {
        updatingFilters = true;
        SearchBox.Clear();
        MinBox.Clear();
        MaxBox.Clear();
        StateFilter.SelectedIndex = DateFilter.SelectedIndex = RoomFilter.SelectedIndex = PhotoFilter.SelectedIndex = PropFilter.SelectedIndex = 0;
        updatingFilters = false;
        ApplyFilters();
    }

    private void Selection_Changed(object sender,SelectionChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        var selected = SelectedBooking;
        CardButton.IsEnabled = selected is not null;
        CancelButton.IsEnabled = session.Office && selected is not null && selected.State is not (BookingState.Cancelled or BookingState.Completed);
        MoveButton.IsEnabled = session.Office && selected is not null && selected.State != BookingState.Cancelled &&
            (session.Admin || selected.State is not (BookingState.Completed or BookingState.InWork));
        MoveButton.Content = selected?.State is BookingState.Completed or BookingState.InWork ? "Редактировать" : "Перенести";
        NewButton.IsEnabled = session.Office && view is not null;
    }

    private async void New_Click(object sender,RoutedEventArgs e) => await RunAsync(() => EditAsync(null));
    private async void Move_Click(object sender,RoutedEventArgs e)
    {
        if (SelectedBooking is { } item) await RunAsync(() => EditAsync(item));
    }

    private async Task EditAsync(Booking? booking)
    {
        var form = new BookingEditor(repo,await repo.ClientsAsync(),await repo.RoomsAsync(),await repo.StaffAsync(true),booking)
        { Owner = this };
        if (form.ShowDialog() == true) await ReloadAsync();
    }

    private async void Cancel_Click(object sender,RoutedEventArgs e)
    {
        if (SelectedBooking is not { } booking) return;
        await RunAsync(async () =>
        {
            var form = new FormWindow("Отменить бронь",booking.Number,
                "Укажите причину отмены. Принятые платежи остаются в журнале.", "Отменить бронь") { Owner = this };
            var reason = Input("",250);
            form.Add("Причина",reason);
            form.OnSubmit(() => repo.CancelAsync(booking.Id,reason.Text));
            if (form.ShowDialog() == true) await ReloadAsync();
        });
    }

    private async void Card_Click(object sender,RoutedEventArgs e)
    {
        if (SelectedBooking is not { } item) return;
        await RunAsync(async () =>
        {
            new ShootWindow(repo,item) { Owner = this }.ShowDialog();
            await ReloadAsync();
        });
    }

    private async void Clients_Click(object sender,RoutedEventArgs e) => await RunAsync(async () =>
    {
        CatalogDialogs.Clients(repo).ShowOwned(this);
        await ReloadAsync();
    });
    private void Rooms_Click(object sender,RoutedEventArgs e) => CatalogDialogs.Rooms(repo).ShowOwned(this);
    private void Props_Click(object sender,RoutedEventArgs e) => CatalogDialogs.Props(repo).ShowOwned(this);
    private void Staff_Click(object sender,RoutedEventArgs e) => CatalogDialogs.Staff(repo).ShowOwned(this);
    private void Reports_Click(object sender,RoutedEventArgs e) => new ReportsWindow(repo).ShowOwned(this);

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        Workspace.IsEnabled = false;
        try { await action(); }
        catch (Exception ex) { UiMessages.ShowError(this,ex); }
        finally
        {
            Workspace.IsEnabled = true;
            busy = false;
            UpdateButtons();
        }
    }

    private void SignOut_Click(object sender,RoutedEventArgs e)
    {
        if (busy || MessageBox.Show(this,"Завершить сеанс?", "Выход", MessageBoxButton.YesNo,
            MessageBoxImage.Question,MessageBoxResult.No) != MessageBoxResult.Yes) return;
        SignedOut = true;
        Close();
    }
}
