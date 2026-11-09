using System.Windows;
using System.Windows.Controls;
using WPF_PR19_fitness_Zaytsev.ApplicationData;
using static WPF_PR19_fitness_Zaytsev.MainPage.DialogKit;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal static class AdminDialogs
{
    public static FormWindow EmployeeEditor(FitnessRepository repo, Employee? employee)
    {
        var form = new FormWindow(employee is null ? "Добавить сотрудника" : "Редактировать сотрудника",
            "СПРАВОЧНИК", employee is null ? "Пароль должен содержать не менее 8 знаков." :
            "Пустое поле пароля сохраняет прежний пароль.", "Сохранить");
        var name = Input(employee?.Name, 100);
        var login = Input(employee?.Login, 50);
        var phone = Input(employee?.Phone, 25);
        var role = EnumChoice<StaffRole>(FitnessText.RoleName, employee?.Role ?? StaffRole.Desk);
        var password = new PasswordBox { MaxLength = 128 };
        var active = new CheckBox { Content = "Учётная запись активна", IsChecked = employee?.IsActive ?? true, MinHeight = 28 };
        form.Add("ФИО", name);
        form.Add(Pair(Field("Логин", login), Field("Телефон", phone)));
        form.Add("Роль", role);
        form.Add("Пароль", password);
        form.Add(active);
        form.OnSubmit(() => repo.SaveEmployeeAsync(employee, name.Text, Selected<StaffRole>(role, "роль"),
            login.Text, phone.Text, active.IsChecked == true, password.Password));
        return form;
    }

    public static FormWindow RoomEditor(FitnessRepository repo, Room? room)
    {
        var form = new FormWindow(room is null ? "Добавить зал" : "Редактировать зал", "СПРАВОЧНИК",
            "Вместимость зала ограничивает число участников тренировки.", "Сохранить");
        var name = Input(room?.Name, 60);
        var capacity = Input(room?.Capacity.ToString(), 10);
        form.Add("Название зала", name);
        form.Add("Вместимость", capacity);
        form.OnSubmit(() => repo.SaveRoomAsync(room, name.Text, PositiveInt(capacity, "Вместимость")));
        return form;
    }

    public static FormWindow TrainingEditor(FitnessRepository repo, IReadOnlyList<Room> rooms,
        IReadOnlyList<Employee> employees, Training? training)
    {
        var form = new FormWindow(training is null ? "Добавить тренировку" : "Редактировать тренировку",
            "РАСПИСАНИЕ", "Зал и тренер не могут быть заняты двумя занятиями одновременно.", "Сохранить", 670);
        var coaches = employees.Where(person => person.Role == StaffRole.Coach && person.IsActive).ToList();
        var name = Input(training?.Name, 100);
        var room = Choices(rooms, value => $"{value.Name} · {value.Capacity} мест",
            rooms.FirstOrDefault(value => value.Id == training?.RoomId), true);
        var coach = Choices(coaches, value => value.Name,
            coaches.FirstOrDefault(value => value.Id == training?.CoachId), true);
        var type = EnumChoice<WorkoutType>(FitnessText.WorkoutName, training?.Type ?? WorkoutType.Group);
        var week = Choices(Enumerable.Range(1, 7), day => FitnessText.Weekdays[day - 1],
            training?.Weekday ?? FitnessText.WeekdayOf(DateTime.Today), true);
        var start = Input(training is null ? "" : FitnessText.Time(training.StartsAt), 5);
        var end = Input(training is null ? "" : FitnessText.Time(training.EndsAt), 5);
        var capacity = Input(training?.MaxParticipants.ToString(), 10);
        form.Add("Название", name);
        form.Add(Pair(Field("Зал", room), Field("Тренер", coach)));
        form.Add(Pair(Field("Тип", type), Field("День недели", week)));
        form.Add(Pair(Field("Начало (ЧЧ:ММ)", start), Field("Окончание (ЧЧ:ММ)", end)));
        form.Add("Лимит участников", capacity);
        form.OnSubmit(() => repo.SaveTrainingAsync(training, name.Text, Selected<Room>(room, "зал").Id,
            Selected<WorkoutType>(type, "тип"), Selected<Employee>(coach, "тренера").Id, PositiveInt(capacity, "Лимит"),
            Selected<int>(week, "день недели"), Time(start), Time(end)));
        return form;
    }
}

internal sealed class DirectoryWindow<T> : ClubWindow where T : class
{
    private readonly DataGrid grid;
    private readonly Func<Task<List<T>>> load;
    private readonly Func<T?, FormWindow> edit;
    private readonly Button editButton;

    public DirectoryWindow(string title, string description, Func<Task<List<T>>> load, Func<T?, FormWindow> edit, DataGrid grid)
        : base(title, "АДМИНИСТРИРОВАНИЕ", description, 855, 510)
    {
        this.load = load;
        this.edit = edit;
        this.grid = grid;
        var actions = new StackPanel { Orientation = Orientation.Horizontal };
        actions.Children.Add(Button("Добавить", async (_, _) => await EditAsync(null)));
        editButton = Button("Редактировать", async (_, _) =>
        {
            if (grid.SelectedItem is T value) await EditAsync(value);
        }, "SecondaryButton");
        editButton.IsEnabled = false;
        actions.Children.Add(editButton);
        AddDocked(actions, Dock.Top);
        AddDocked(ErrorText, Dock.Bottom);
        Layout.Children.Add(grid);
        grid.SelectionChanged += (_, _) => editButton.IsEnabled = grid.SelectedItem is T;
        Loaded += async (_, _) => await ReloadAsync();
    }

    private async Task ReloadAsync()
    {
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            var rows = await load();
            grid.ItemsSource = rows;
            if (rows.Count == 0) ErrorText.Text = "Справочник пуст.";
        }
        catch (Exception exception) { ErrorText.Text = UiMessages.ErrorText(exception); }
        finally { Layout.IsEnabled = true; }
    }

    private async Task EditAsync(T? value)
    {
        var form = edit(value);
        form.Owner = this;
        if (form.ShowDialog() == true) await ReloadAsync();
    }
}

internal sealed class ReportWindow : ClubWindow
{
    private readonly FitnessRepository repo;
    private readonly TextBox start = Input(DateText(DateTime.Today.AddDays(-30)), 10);
    private readonly TextBox end = Input(DateText(DateTime.Today), 10);
    private readonly StackPanel results = new();

    public ReportWindow(FitnessRepository repo) : base("Отчёты", "АДМИНИСТРИРОВАНИЕ",
        "Посещения и выручка от разовых входов за выбранный период.", 680)
    {
        this.repo = repo;
        AddDocked(Pair(Field("Начало периода", start), Field("Конец периода", end)), Dock.Top);
        AddDocked(Button("Сформировать", async (_, _) => await RefreshAsync()), Dock.Top);
        AddDocked(ErrorText, Dock.Bottom);
        results.Margin = new Thickness(0, 10, 0, 0);
        Layout.Children.Add(results);
        Loaded += async (_, _) => await RefreshAsync();
    }

    private async Task RefreshAsync()
    {
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            var report = await repo.ReportAsync(Date(start, "начало периода"), Date(end, "конец периода"));
            results.Children.Clear();
            results.Children.Add(Note($"Клиентов всего: {report.Clients}     Активных абонементов на сегодня: {report.ActivePlans}"));
            results.Children.Add(Note($"Посещений: {report.Visits}     Групповых: {report.GroupVisits}     Гостевых: {report.Guests}"));
            results.Children.Add(Note($"Выручка от разовых входов: {report.SingleRevenue.ToString("N2", FitnessText.Russian)} ₽"));
        }
        catch (Exception exception)
        {
            results.Children.Clear();
            ErrorText.Text = UiMessages.ErrorText(exception);
        }
        finally { Layout.IsEnabled = true; }
    }
}
