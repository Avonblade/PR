using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WPF_PR19_fitness_Zaytsev.ApplicationData;
using static WPF_PR19_fitness_Zaytsev.MainPage.DialogKit;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal sealed class ScheduleWindow : ClubWindow
{
    private readonly FitnessRepository repo;
    private readonly StaffSession session;
    private readonly DataGrid grid;
    private readonly TextBox search = Input("", 100);
    private readonly ComboBox days;
    private readonly ComboBox rooms;
    private readonly TextBlock weekText = Note("");
    private readonly TextBlock count = Note("");
    private readonly Button book;
    private readonly Button attend;
    private readonly Button edit;
    private List<ScheduleRow> rows = [];
    private DateTime week;
    private bool busy;

    public ScheduleWindow(FitnessRepository repo, IReadOnlyList<Room> roomList, DateTime day)
        : base("Расписание", "ТРЕНИРОВКИ", "Занятия на выбранную неделю. Запись и отметки относятся к дате занятия.", 1120, 620)
    {
        this.repo = repo;
        session = repo.Session!;
        week = day.Date.AddDays(1 - FitnessText.WeekdayOf(day));
        var filters = new Grid();
        foreach (var width in new[] { new GridLength(1, GridUnitType.Star), new GridLength(160), new GridLength(172), GridLength.Auto })
            filters.ColumnDefinitions.Add(new ColumnDefinition { Width = width });
        days = Choices(Enumerable.Range(0, 8), index => index == 0 ? "Все дни" : FitnessText.Weekdays[index - 1], 0, true);
        rooms = Choices(new[] { new Room(0, "Все залы", 0, []) }.Concat(roomList), room => room.Name);
        rooms.SelectedIndex = 0;
        AddFilter(0, "Поиск тренировки", search);
        AddFilter(1, "День недели", days);
        AddFilter(2, "Зал", rooms);
        var reset = Button("Сбросить фильтры", (_, _) => { search.Clear(); days.SelectedIndex = rooms.SelectedIndex = 0; }, "SecondaryButton");
        reset.VerticalAlignment = VerticalAlignment.Bottom;
        reset.Margin = new Thickness(0, 0, 0, 8);
        Grid.SetColumn(reset, 3);
        filters.Children.Add(reset);
        AddDocked(filters, Dock.Top);

        var toolbar = new DockPanel { LastChildFill = false };
        var weekControls = new StackPanel { Orientation = Orientation.Horizontal };
        weekControls.Children.Add(Button("‹", async (_, _) => await MoveWeekAsync(-7), "SecondaryButton"));
        weekText.Margin = new Thickness(0, 0, 8, 0);
        weekText.VerticalAlignment = VerticalAlignment.Center;
        weekControls.Children.Add(weekText);
        weekControls.Children.Add(Button("›", async (_, _) => await MoveWeekAsync(7), "SecondaryButton"));
        toolbar.Children.Add(weekControls);
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        book = Button("Записать клиента", async (_, _) => await OpenBookingAsync());
        attend = Button("Присутствие", async (_, _) => await OpenAttendanceAsync(), "SecondaryButton");
        edit = Button("Редактировать", async (_, _) => await OpenEditorAsync(false), "SecondaryButton");
        if (session.CanSell) actions.Children.Add(book);
        if (session.CanMark) actions.Children.Add(attend);
        if (session.Admin)
        {
            actions.Children.Add(Button("Добавить", async (_, _) => await OpenEditorAsync(true), "SecondaryButton"));
            actions.Children.Add(edit);
        }
        DockPanel.SetDock(actions, Dock.Right);
        toolbar.Children.Add(actions);
        AddDocked(toolbar, Dock.Top);

        var footer = new StackPanel();
        footer.Children.Add(count);
        footer.Children.Add(ErrorText);
        AddDocked(footer, Dock.Bottom);

        grid = Table(("Дата", "SessionOn", 100, "dd.MM.yyyy"), ("День недели", "WeekdayText", 120, null),
            ("Тренировка", "Name", 0, null), ("Время", "Time", 104, null), ("Зал", "Room", 119, null),
            ("Тип", "Type", 100, null), ("Тренер", "Coach", 0, null), ("Мин.", "Duration", 52, null),
            ("Мест", "Capacity", 58, null), ("Записано", "Booked", 87, null));
        Layout.Children.Add(grid);
        grid.SelectionChanged += (_, _) => UpdateActions();
        search.TextChanged += (_, _) => ApplyFilters();
        days.SelectionChanged += (_, _) => ApplyFilters();
        rooms.SelectionChanged += (_, _) => ApplyFilters();
        Loaded += async (_, _) => await ReloadAsync();
        UpdateActions();

        void AddFilter(int column, string label, FrameworkElement input)
        {
            var field = Field(label, input);
            field.Margin = new Thickness(0, 0, 8, 8);
            Grid.SetColumn(field, column);
            filters.Children.Add(field);
        }
    }

    private ScheduleRow? SelectedRow => grid.SelectedItem as ScheduleRow;

    private async Task MoveWeekAsync(int days)
    {
        if (busy) return;
        week = week.AddDays(days);
        await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        int? previous = SelectedRow?.Training.Id;
        await RunAsync(async () =>
        {
            weekText.Text = $"{DateText(week)} – {DateText(week.AddDays(6))}";
            rows = await repo.ScheduleAsync(week);
            grid.ItemsSource = rows;
            ApplyFilters();
            grid.SelectedItem = rows.FirstOrDefault(row => row.Training.Id == previous && grid.Items.Contains(row));
        });
    }

    private void ApplyFilters()
    {
        if (grid.ItemsSource is null) return;
        var view = CollectionViewSource.GetDefaultView(rows);
        string term = search.Text.Trim();
        int weekday = (days.SelectedItem as Option<int>)?.Value ?? 0;
        int room = Selection<Room>(rooms)?.Id ?? 0;
        view.Filter = item => item is ScheduleRow row &&
            row.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) &&
            (weekday == 0 || row.Weekday == weekday) && (room == 0 || row.Training.RoomId == room);
        count.Text = view.IsEmpty ? "Занятий по выбранным условиям нет." : $"Показано {grid.Items.Count} из {rows.Count}";
    }

    private void UpdateActions()
    {
        var row = SelectedRow;
        book.IsEnabled = row is not null && row.SessionOn + row.Training.EndsAt > DateTime.Now;
        attend.IsEnabled = row is not null && (session.Admin || row.Training.CoachId == session.Id);
        edit.IsEnabled = row is not null;
    }

    private async Task OpenBookingAsync()
    {
        if (SelectedRow is not { } row) return;
        bool changed = false;
        await RunAsync(async () =>
        {
            var clients = await repo.ClientsAsync();
            var form = GroupDialogs.Book(repo, rows, clients, row);
            form.Owner = this;
            changed = form.ShowDialog() == true;
        });
        if (changed) await ReloadAsync();
    }

    private async Task OpenAttendanceAsync()
    {
        if (SelectedRow is not { } row) return;
        await RunAsync(async () =>
        {
            var bookings = await repo.BookingsAsync(row.Training.Id, row.SessionOn);
            new AttendanceWindow(repo, row, bookings) { Owner = this }.ShowDialog();
        });
    }

    private async Task OpenEditorAsync(bool create)
    {
        var row = SelectedRow;
        if (!create && row is null) return;
        bool changed = false;
        await RunAsync(async () =>
        {
            var form = AdminDialogs.TrainingEditor(repo, await repo.RoomsAsync(), await repo.EmployeesAsync(), create ? null : row!.Training);
            form.Owner = this;
            changed = form.ShowDialog() == true;
        });
        if (changed) await ReloadAsync();
    }

    private async Task RunAsync(Func<Task> action)
    {
        if (busy) return;
        busy = true;
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        try { await action(); }
        catch (Exception exception) { ErrorText.Text = UiMessages.ErrorText(exception); }
        finally
        {
            Layout.IsEnabled = true;
            busy = false;
            UpdateActions();
        }
    }
}

internal static class GroupDialogs
{
    public static FormWindow Book(FitnessRepository repo, IReadOnlyList<ScheduleRow> schedule,
        IReadOnlyList<ClientRow> clients, ScheduleRow selected)
    {
        var form = new FormWindow("Запись на тренировку", "ТРЕНИРОВКИ",
            "Выберите дату занятия из расписания и клиента. Гостевая запись доступна без абонемента.", "Записать", 660);
        var eligible = schedule.Where(row => row.SessionOn + row.Training.EndsAt > DateTime.Now).ToList();
        var training = Choices(eligible, row => $"{row.Name} · {DateText(row.SessionOn)} · {row.Time}", selected, true);
        var client = ClientPicker(clients, null);
        var guest = new CheckBox { Content = "Гостевая запись", MinHeight = 30 };
        var capacity = Note("");
        form.Add("Тренировка и дата", training);
        form.Add(capacity);
        form.Add("Поиск и выбор клиента", client);
        form.Add(guest);
        void Update() => capacity.Text = Selection<ScheduleRow>(training) is { } row
            ? $"Зал: {row.Room}. Записано {row.Booked} из {row.Capacity}." : "Выберите занятие.";
        training.SelectionChanged += (_, _) => Update();
        Update();
        form.OnSubmit(() =>
        {
            var row = Selected<ScheduleRow>(training, "тренировку");
            var person = client.Value ?? throw new FitnessRuleException("Выберите клиента.");
            return repo.BookAsync(row.Training.Id, row.SessionOn, person.Client.Id, guest.IsChecked == true);
        });
        return form;
    }
}

internal sealed class AttendanceWindow : ClubWindow
{
    private readonly FitnessRepository repo;
    private readonly ScheduleRow schedule;
    private readonly DataGrid grid;
    private readonly Button present;
    private readonly Button absent;
    private readonly TextBlock count = Note("");

    public AttendanceWindow(FitnessRepository repo, ScheduleRow schedule, IReadOnlyList<BookingRow> bookings)
        : base("Отметка присутствия", "ТРЕНИРОВКИ",
            $"{schedule.Name} · {DateText(schedule.SessionOn)} · {schedule.Time} · {schedule.Coach}", 770, 470)
    {
        this.repo = repo;
        this.schedule = schedule;
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        present = Button("Пришёл", async (_, _) => await MarkAsync(Attendance.Present));
        absent = Button("Не пришёл", async (_, _) => await MarkAsync(Attendance.Absent), "SecondaryButton");
        actions.Children.Add(present);
        actions.Children.Add(absent);
        AddDocked(actions, Dock.Top);
        var footer = new StackPanel();
        footer.Children.Add(count);
        footer.Children.Add(ErrorText);
        AddDocked(footer, Dock.Bottom);

        grid = Table(("Участник", "Client", 0, null), ("Доступ", "AccessText", 165, null), ("Отметка", "AttendanceText", 175, null));
        Layout.Children.Add(grid);
        grid.SelectionChanged += (_, _) => UpdateActions();
        Apply(bookings);
    }

    private void Apply(IReadOnlyList<BookingRow> rows)
    {
        grid.ItemsSource = rows;
        count.Text = rows.Count == 0 ? "На это занятие никто не записан." : $"Записано: {rows.Count} из {schedule.Capacity}.";
        UpdateActions();
    }

    private void UpdateActions()
    {
        present.IsEnabled = absent.IsEnabled = grid.SelectedItem is BookingRow { State: Attendance.Pending } &&
            schedule.SessionOn + schedule.Training.StartsAt <= DateTime.Now;
    }

    private async Task MarkAsync(Attendance state)
    {
        if (grid.SelectedItem is not BookingRow row) return;
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            await repo.MarkAsync(row.Id, state);
            Apply(await repo.BookingsAsync(schedule.Training.Id, schedule.SessionOn));
        }
        catch (Exception exception) { ErrorText.Text = UiMessages.ErrorText(exception); }
        finally { Layout.IsEnabled = true; }
    }
}
