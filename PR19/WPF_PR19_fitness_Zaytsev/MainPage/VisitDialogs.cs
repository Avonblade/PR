using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using WPF_PR19_fitness_Zaytsev.ApplicationData;
using static WPF_PR19_fitness_Zaytsev.MainPage.DialogKit;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal static class VisitDialogs
{
    public static FormWindow Entry(FitnessRepository repo, IReadOnlyList<ClientRow> clients, ClientRow? selected)
    {
        var form = new FormWindow("Регистрация входа", "СТОЙКА КЛУБА",
            "Поиск по ФИО, телефону или карте. Время входа фиксируется при сохранении.", "Зарегистрировать");
        var picker = ClientPicker(clients, selected);
        AccessType[] accessTypes = [AccessType.Membership, AccessType.Single, AccessType.Guest];
        var access = Choices(accessTypes, FitnessText.AccessName, AccessType.Single, true);
        var cost = Input("", 10);
        var status = Note("");
        var costField = Field("Стоимость разового входа, ₽", cost);
        AutomationProperties.SetAutomationId(access, "AccessType");
        AutomationProperties.SetAutomationId(cost, "EntryPrice");
        form.Add("Поиск и выбор клиента", picker);
        form.Add(status);
        form.Add("Тип доступа", access);
        form.Add(costField);

        void UpdateClient()
        {
            var client = picker.Value;
            var plan = client?.Plan;
            bool hasPlan = client?.Client.Status == ClientStatus.Active &&
                plan?.StateOn(DateTime.Today) == PlanState.Active;
            access.SelectedIndex = hasPlan ? 0 : 1;
            status.Text = client is null ? "Клиент не выбран." :
                client.IsBlocked ? "Клиент заблокирован. Вход запрещён." :
                hasPlan ? $"Абонемент: {plan!.TypeText}. Остаток: {client.RemainingText}." :
                "Активный абонемент отсутствует. Можно выбрать разовый или гостевой вход.";
        }

        void UpdateCost() => costField.Visibility = access.SelectedIndex == 1 ? Visibility.Visible : Visibility.Collapsed;
        picker.SelectionChanged += (_, _) => UpdateClient();
        access.SelectionChanged += (_, _) => UpdateCost();
        UpdateClient();
        UpdateCost();
        form.OnSubmit(() =>
        {
            var client = picker.Value ?? throw new FitnessRuleException("Выберите клиента.");
            var type = Selected<AccessType>(access, "тип доступа");
            return repo.EnterAsync(client.Client.Id, type, type == AccessType.Single ? PositiveAmount(cost) : null);
        });
        return form;
    }

    public static FormWindow Exit(FitnessRepository repo, IReadOnlyList<VisitRow> visits)
    {
        var form = new FormWindow("Фиксация выхода", "СТОЙКА КЛУБА",
            "Выберите открытое посещение. Длительность рассчитывается по времени входа и выхода.", "Зафиксировать выход", 700);
        var choice = Choices(visits, visit => visit.Caption, visits.FirstOrDefault(), true);
        var time = Input(StampText(DateTime.Now), 19);
        var preview = Note(visits.Count == 0 ? "Открытых посещений нет." : "");
        AutomationProperties.SetAutomationId(time, "ExitTime");
        form.Add("Открытое посещение", choice);
        form.Add("Время выхода (ДД.ММ.ГГГГ ЧЧ:ММ:СС)", time);
        form.Add(preview);

        void Update()
        {
            if (Selection<VisitRow>(choice) is not { } visit) return;
            try
            {
                TimeSpan span = Stamp(time) - visit.EnteredAt;
                preview.Text = span < TimeSpan.Zero ? "Выход раньше входа." :
                    $"Длительность: {(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}.";
            }
            catch (FitnessRuleException) { preview.Text = "Проверьте формат времени выхода."; }
        }

        choice.SelectionChanged += (_, _) => Update();
        time.TextChanged += (_, _) => Update();
        Update();
        form.OnSubmit(() => repo.ExitAsync(Selected<VisitRow>(choice, "посещение").Id, Stamp(time)));
        return form;
    }
}

internal sealed class HistoryWindow : ClubWindow
{
    public HistoryWindow(string? client, IReadOnlyList<VisitRow> visits) : base("История посещений", "ЖУРНАЛ",
        client is null ? "Все зарегистрированные входы, выходы и групповые занятия." : $"Посещения клиента {client}.", 990, 565)
    {
        var grid = Table(("Клиент", "Client", 0, null), ("Вход", "EnteredAt", 147, "dd.MM.yyyy HH:mm:ss"),
            ("Выход", "ExitedAt", 147, "dd.MM.yyyy HH:mm:ss"), ("Доступ", "AccessText", 158, null),
            ("Длительность", "DurationText", 111, null), ("Стоимость, ₽", "Price", 100, "N2"),
            ("Тренер", "Coach", 0, null));
        grid.ItemsSource = visits;
        if (visits.Count == 0) AddDocked(Note("В журнале пока нет посещений."), Dock.Top);
        Layout.Children.Add(grid);
    }
}
