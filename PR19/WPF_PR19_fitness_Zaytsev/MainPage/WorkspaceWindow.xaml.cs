using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;
using WPF_PR19_fitness_Zaytsev.ApplicationData;
using static WPF_PR19_fitness_Zaytsev.MainPage.DialogKit;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

public partial class WorkspaceWindow : Window
{
    private readonly FitnessRepository repo;
    private readonly StaffSession session;
    private readonly DispatcherTimer clock = new() { Interval = TimeSpan.FromSeconds(1) };
    private List<ClientRow> rows = [];
    private ICollectionView? view;
    private bool busy;
    private bool resetting;
    private DateTime shownDate = DateTime.Today;
    internal bool SignedOut { get; private set; }

    internal WorkspaceWindow(FitnessRepository repo)
    {
        this.repo = repo;
        session = repo.Session ?? throw new FitnessRuleException("Войдите в систему.");
        InitializeComponent();
        UserText.Text = session.Name;
        RoleText.Text = session.Role.RoleName();
        ClockText.Text = StampText(DateTime.Now);
        clock.Tick += Clock_Tick;
        Closed += (_, _) => clock.Stop();

        ClientPanel.Visibility = Visible(session.CanViewClients);
        CoachPanel.Visibility = Visible(!session.CanViewClients);
        EntryButton.Visibility = ExitButton.Visibility = Visible(session.CanRegister);
        HistoryButton.Visibility = Visible(session.CanViewClients);
        EmployeesButton.Visibility = RoomsButton.Visibility = ReportsButton.Visibility = Visible(session.Admin);
        AddButton.Visibility = BlockButton.Visibility = ThawButton.Visibility = Visible(session.Admin);
        EditButton.Visibility = SaleButton.Visibility = FreezeButton.Visibility = Visible(session.CanSell);

        SetOptions(StatusFilter, new string[] { "Все статусы" }.Concat(Enum.GetValues<ClientStatus>().Select(FitnessText.ClientStatusName)));
        SetOptions(TypeFilter, new string[] { "Все типы" }.Concat(Enum.GetValues<PlanType>().Select(FitnessText.PlanTypeName)));
        SetOptions(ExpiryFilter, ["Любой срок", "В течение 7 дней", "В течение 30 дней", "Истёк"]);
        SetOptions(BalanceFilter, ["Любой остаток", "0", "1–3", "Больше 3"]);
        UpdateActions();
    }

    private static Visibility Visible(bool show) => show ? Visibility.Visible : Visibility.Collapsed;

    private static void SetOptions(ComboBox box, IEnumerable<string> options)
    {
        box.ItemsSource = options.ToArray();
        box.SelectedIndex = 0;
    }

    private ClientRow? SelectedClient => ClientsGrid.SelectedItem as ClientRow;

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        clock.Start();
        if (session.CanViewClients) await ReloadAsync();
    }

    private async void Clock_Tick(object? sender, EventArgs e)
    {
        ClockText.Text = StampText(DateTime.Now);
        if (session.CanViewClients && shownDate != DateTime.Today && !busy && IsActive)
            await ReloadAsync();
    }

    private Task ReloadAsync() => RunAsync(LoadClientsAsync);

    private async Task LoadClientsAsync()
    {
        var previous = SelectedClient?.Client.Id;
        var sort = view?.SortDescriptions.ToArray() ?? [];
        rows = await repo.ClientsAsync();
        view = CollectionViewSource.GetDefaultView(rows);
        ClientsGrid.ItemsSource = view;
        foreach (var description in sort) view.SortDescriptions.Add(description);
        foreach (var column in ClientsGrid.Columns)
        {
            column.SortDirection = sort.Where(s => s.PropertyName == column.SortMemberPath)
                .Select(s => (ListSortDirection?)s.Direction).FirstOrDefault();
        }
        shownDate = DateTime.Today;
        ApplyFilters();
        ClientsGrid.SelectedItem = rows.FirstOrDefault(row => row.Client.Id == previous && view.Contains(row));
        StatusText.Text = $"Обновлено в {DateTime.Now:HH:mm:ss}";
    }

    private void Search_Changed(object sender, TextChangedEventArgs e) => ApplyFilters();
    private void Filter_Changed(object sender, SelectionChangedEventArgs e) => ApplyFilters();

    private void ApplyFilters()
    {
        if (view is null || resetting) return;
        var today = DateTime.Today;
        string search = SearchBox.Text.Trim();
        int status = StatusFilter.SelectedIndex;
        int type = TypeFilter.SelectedIndex;
        int expiry = ExpiryFilter.SelectedIndex;
        int balance = BalanceFilter.SelectedIndex;

        view.Filter = item => item is ClientRow row &&
            (row.Client.Matches(search) || row.PlanTypeText.Contains(search, StringComparison.CurrentCultureIgnoreCase)) &&
            (status <= 0 || (int)row.Client.Status == status - 1) &&
            (type <= 0 || row.Plan is { } plan && (int)plan.Type == type - 1) &&
            MatchesExpiry(row.Plan, today, expiry) && MatchesBalance(row.Remaining, balance);

        CountText.Text = $"Показано {ClientsGrid.Items.Count} из {rows.Count}";
        EmptyText.Text = rows.Count == 0 ? "В базе пока нет клиентов." : "По выбранным условиям клиенты не найдены.";
        EmptyText.Visibility = Visible(view.IsEmpty);
    }

    private static bool MatchesExpiry(Membership? plan, DateTime today, int filter) => filter switch
    {
        1 => plan?.StateOn(today) == PlanState.Active && plan.EndsOn <= today.AddDays(7),
        2 => plan?.StateOn(today) == PlanState.Active && plan.EndsOn <= today.AddDays(30),
        3 => plan?.StateOn(today) == PlanState.Expired,
        _ => true
    };

    private static bool MatchesBalance(int? remaining, int filter) => filter switch
    {
        1 => remaining == 0,
        2 => remaining is >= 1 and <= 3,
        3 => remaining > 3,
        _ => true
    };

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        resetting = true;
        SearchBox.Clear();
        StatusFilter.SelectedIndex = TypeFilter.SelectedIndex = ExpiryFilter.SelectedIndex = BalanceFilter.SelectedIndex = 0;
        resetting = false;
        ApplyFilters();
    }

    private void Client_Changed(object sender, SelectionChangedEventArgs e) => UpdateActions();

    private void UpdateActions()
    {
        var row = SelectedClient;
        bool loaded = view is not null;
        AddButton.IsEnabled = loaded && session.Admin;
        SaleButton.IsEnabled = loaded && session.CanSell;
        EntryButton.IsEnabled = loaded && session.CanRegister;
        EditButton.IsEnabled = loaded && row is not null && session.CanSell;
        BlockButton.IsEnabled = loaded && row is not null && session.Admin;
        BlockButton.Content = row?.IsBlocked == true ? "Разблокировать" : "Заблокировать";
        PlansButton.IsEnabled = loaded && row is not null;
        FreezeButton.IsEnabled = session.CanSell && row?.Plan?.StateOn(DateTime.Today) == PlanState.Active;
        ThawButton.IsEnabled = session.Admin && row?.Plan?.StateOn(DateTime.Today) == PlanState.Frozen;
    }

    private async void Refresh_Click(object sender, RoutedEventArgs e) => await ReloadAsync();
    private async void Add_Click(object sender, RoutedEventArgs e) => await OpenFormAsync(() => ClientDialogs.Edit(repo, null));
    private async void Edit_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClient is { } row) await OpenFormAsync(() => ClientDialogs.Edit(repo, row.Client));
    }

    private async void Block_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClient is not { } row) return;
        string action = row.IsBlocked ? "Разблокировать" : "Заблокировать";
        if (MessageBox.Show(this, $"{action} клиента «{row.Name}»?", action, MessageBoxButton.YesNo,
            MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;

        await RunAsync(async () =>
        {
            await repo.ToggleBlockAsync(row.Client);
            await LoadClientsAsync();
        });
    }

    private async void Sale_Click(object sender, RoutedEventArgs e) =>
        await OpenFormAsync(() => ClientDialogs.Sale(repo, rows, SelectedClient));

    private async void Freeze_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClient is not { } row) return;
        await OpenFormAsync(async () => ClientDialogs.Freeze(repo, row, await repo.PlansAsync(row.Client.Id)));
    }

    private async void Thaw_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClient is not { } row) return;
        await OpenFormAsync(async () => ClientDialogs.PlanAction(repo, row, await repo.PlansAsync(row.Client.Id), true));
    }

    private async void Plans_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedClient is not { } row) return;
        await RunAsync(async () =>
        {
            var window = new PlansWindow(repo, row, await repo.PlansAsync(row.Client.Id)) { Owner = this };
            window.ShowDialog();
            if (window.Changed) await LoadClientsAsync();
        });
    }

    private async void Entry_Click(object sender, RoutedEventArgs e) =>
        await OpenFormAsync(() => VisitDialogs.Entry(repo, rows, SelectedClient));

    private async void Exit_Click(object sender, RoutedEventArgs e) =>
        await OpenFormAsync(async () => VisitDialogs.Exit(repo, await repo.VisitsAsync(openOnly: true)));

    private async void History_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        var row = SelectedClient;
        new HistoryWindow(row?.Name, await repo.VisitsAsync(row?.Client.Id)) { Owner = this }.ShowDialog();
    });

    private async void Schedule_Click(object sender, RoutedEventArgs e) => await RunAsync(async () =>
    {
        new ScheduleWindow(repo, await repo.RoomsAsync(), DateTime.Today) { Owner = this }.ShowDialog();
        if (session.CanViewClients) await LoadClientsAsync();
    });

    private void Employees_Click(object sender, RoutedEventArgs e) =>
        new DirectoryWindow<Employee>("Сотрудники", "Учётные записи, роли и контактные данные.",
            repo.EmployeesAsync, employee => AdminDialogs.EmployeeEditor(repo, employee),
            Table(("ФИО", "Name", 0, null), ("Логин", "Login", 115, null), ("Роль", "RoleText", 160, null),
                ("Телефон", "Phone", 155, null), ("Активен", "ActiveText", 90, null))) { Owner = this }.ShowDialog();

    private void Rooms_Click(object sender, RoutedEventArgs e) =>
        new DirectoryWindow<Room>("Залы", "Вместимость залов для групповых и индивидуальных занятий.",
            repo.RoomsAsync, room => AdminDialogs.RoomEditor(repo, room),
            Table(("Название", "Name", 0, null), ("Вместимость", "Capacity", 160, null))) { Owner = this }.ShowDialog();

    private void Reports_Click(object sender, RoutedEventArgs e) => new ReportWindow(repo) { Owner = this }.ShowDialog();

    private Task OpenFormAsync(Func<FormWindow> create) => OpenFormAsync(() => Task.FromResult(create()));

    private Task OpenFormAsync(Func<Task<FormWindow>> create) => RunAsync(async () =>
    {
        var form = await create();
        form.Owner = this;
        if (form.ShowDialog() == true) await LoadClientsAsync();
    });

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        Workspace.IsEnabled = false;
        try { await action(); }
        catch (Exception exception)
        {
            StatusText.Text = UiMessages.ErrorText(exception);
            UiMessages.ShowError(this, exception);
        }
        finally
        {
            Workspace.IsEnabled = true;
            busy = false;
            UpdateActions();
        }
    }

    private void SignOut_Click(object sender, RoutedEventArgs e)
    {
        if (busy || MessageBox.Show(this, "Завершить сеанс и вернуться к окну входа?", "Выход",
            MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
        SignedOut = true;
        Close();
    }
}
