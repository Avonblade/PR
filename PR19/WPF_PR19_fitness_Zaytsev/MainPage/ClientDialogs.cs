using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using WPF_PR19_fitness_Zaytsev.ApplicationData;
using static WPF_PR19_fitness_Zaytsev.MainPage.DialogKit;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal static class ClientDialogs
{
    public static FormWindow Edit(FitnessRepository repo, ClientProfile? client)
    {
        bool admin = repo.Session!.Admin;
        var form = new FormWindow(client is null ? "Добавить клиента" : "Редактировать клиента",
            "КАРТОЧКА КЛИЕНТА", admin ? "Укажите контактные данные и статус." : "Менеджер может обновить телефон и почту.", "Сохранить");
        var name = Input(client?.Name, 100, !admin);
        var birth = Input(client is null ? "" : DateText(client.BirthDate), 10, !admin);
        var phone = Input(client?.Phone, 25);
        var email = Input(client?.Email, 100);
        var card = Input(client?.Card, 30, !admin);
        var status = EnumChoice<ClientStatus>(FitnessText.ClientStatusName, client?.Status ?? ClientStatus.Active);
        status.IsEnabled = admin;
        form.Add("ФИО", name);
        form.Add(Pair(Field("Дата рождения (ДД.ММ.ГГГГ)", birth), Field("Телефон", phone)));
        form.Add("Электронная почта", email);
        form.Add(Pair(Field("Номер карты", card), Field("Статус", status)));
        form.OnSubmit(() => repo.SaveClientAsync(client, name.Text,
            admin ? Date(birth, "дату рождения") : client!.BirthDate, phone.Text, email.Text, card.Text,
            Selected<ClientStatus>(status, "статус")));
        return form;
    }

    public static FormWindow Sale(FitnessRepository repo, IReadOnlyList<ClientRow> clients, ClientRow? client)
    {
        var form = new FormWindow("Оформить абонемент", "ПРОДАЖА / ПРОДЛЕНИЕ",
            "Найдите клиента по ФИО или телефону. Срок действия рассчитывается по типу абонемента.", "Оформить");
        var picker = ClientPicker(clients, client);
        var type = EnumChoice<PlanType>(FitnessText.PlanTypeName, PlanType.Month);
        var limited = new CheckBox { Content = "Ограничить число посещений", MinHeight = 29 };
        var limit = Input("", 10);
        var start = Input(DateText(DateTime.Today), 10);
        var end = Input("", 10, true);
        var summary = Note("");
        AutomationProperties.SetAutomationId(type, "PlanType");
        AutomationProperties.SetAutomationId(limited, "UseLimit");
        AutomationProperties.SetAutomationId(limit, "Limit");
        AutomationProperties.SetAutomationId(start, "StartsOn");
        AutomationProperties.SetAutomationId(end, "EndsOn");
        form.Add("Поиск и выбор клиента", picker);
        form.Add(summary);
        form.Add("Тип абонемента", type);
        form.Add(Pair(Field("Начало (ДД.ММ.ГГГГ)", start), Field("Окончание", end)));
        form.Add(limited);
        form.Add("Лимит посещений", limit);

        void UpdateDates()
        {
            try
            {
                DateTime date = Date(start, "дату начала");
                end.Text = date.Year <= 2099 ? DateText(FitnessRepository.PlanEnd(Selected<PlanType>(type, "тип"), date)) : "";
            }
            catch (FitnessRuleException) { end.Clear(); }
        }

        void UpdateLimit()
        {
            bool single = Selected<PlanType>(type, "тип") == PlanType.Single;
            if (single)
            {
                limited.IsChecked = true;
                limit.Text = "1";
            }
            limited.IsEnabled = !single;
            limit.IsEnabled = !single && limited.IsChecked == true;
        }

        void UpdateClient()
        {
            var plan = picker.Value?.Plan;
            summary.Text = picker.Value is null ? "Клиент не выбран." :
                plan is null ? "У клиента пока нет абонементов." :
                $"В карточке: {plan.TypeText.ToLower(FitnessText.Russian)}, {plan.StateText.ToLower(FitnessText.Russian)}, до {DateText(plan.EndsOn)}.";
        }

        picker.SelectionChanged += (_, _) => UpdateClient();
        type.SelectionChanged += (_, _) => { UpdateDates(); UpdateLimit(); };
        limited.Checked += (_, _) => UpdateLimit();
        limited.Unchecked += (_, _) => UpdateLimit();
        start.TextChanged += (_, _) => UpdateDates();
        UpdateClient();
        UpdateDates();
        UpdateLimit();

        form.OnSubmit(() =>
        {
            var chosen = picker.Value ?? throw new FitnessRuleException("Выберите клиента.");
            var planType = Selected<PlanType>(type, "тип абонемента");
            DateTime starts = Date(start, "дату начала");
            return repo.SellAsync(chosen.Client.Id, planType, starts, limited.IsChecked == true
                ? PositiveInt(limit, "Лимит посещений") : null);
        });
        return form;
    }

    public static FormWindow Freeze(FitnessRepository repo, ClientRow client, IReadOnlyList<Membership> plans)
    {
        var form = new FormWindow("Заморозить абонемент", "АБОНЕМЕНТЫ",
            "Укажите период после окончания действующего абонемента. Окончание продлится на число дней заморозки.", "Заморозить");
        var active = plans.Where(p => p.StateOn(DateTime.Today) == PlanState.Active).ToList();
        var choice = Choices(active, p => p.Caption, active.FirstOrDefault(), true);
        var start = Input("", 10);
        var end = Input("", 10);
        var reason = EnumChoice<FreezeReason>(FitnessText.FreezeReasonName, FreezeReason.Holiday);
        var preview = Note("");
        AutomationProperties.SetAutomationId(start, "FreezeStart");
        AutomationProperties.SetAutomationId(end, "FreezeEnd");
        form.Add(Note(client.Name));
        form.Add("Активный абонемент", choice);
        form.Add(Pair(Field("Начало (ДД.ММ.ГГГГ)", start), Field("Окончание (ДД.ММ.ГГГГ)", end)));
        form.Add("Причина", reason);
        form.Add(preview);

        void Update()
        {
            var plan = Selection<Membership>(choice);
            try
            {
                DateTime from = Date(start, "начало");
                DateTime to = Date(end, "окончание");
                int days = (to - from).Days + 1;
                preview.Text = plan is not null && days > 0 && from > plan.EndsOn && to.Year <= 2099
                    ? $"Дней заморозки: {days}. Новое окончание: {DateText(plan.EndsOn.AddDays(days))}."
                    : "Выберите непересекающийся период после окончания абонемента.";
            }
            catch (FitnessRuleException)
            {
                preview.Text = "Укажите обе даты заморозки.";
            }
        }

        if (active.FirstOrDefault() is { } plan)
        {
            start.Text = DateText(plan.EndsOn.AddDays(1));
            end.Text = DateText(plan.EndsOn.AddDays(7));
        }
        start.TextChanged += (_, _) => Update();
        end.TextChanged += (_, _) => Update();
        Update();

        form.OnSubmit(() => repo.FreezeAsync(Selected<Membership>(choice, "активный абонемент").Id,
            Date(start, "начало заморозки"), Date(end, "окончание заморозки"),
            Selected<FreezeReason>(reason, "причину")));
        return form;
    }

    public static FormWindow PlanAction(FitnessRepository repo, ClientRow client, IReadOnlyList<Membership> plans, bool thaw)
    {
        string title = thaw ? "Разморозить абонемент" : "Отменить абонемент";
        var form = new FormWindow(title, "АБОНЕМЕНТЫ",
            thaw ? "Дата окончания после разморозки сохранится." : "При отмене абонемент перестанет давать доступ в клуб.", title.Split(' ')[0]);
        var eligible = plans.Where(plan => thaw ? plan.StateOn(DateTime.Today) == PlanState.Frozen :
            plan.StateOn(DateTime.Today) is not (PlanState.Cancelled or PlanState.Expired)).ToList();
        var choice = Choices(eligible, p => p.Caption, eligible.FirstOrDefault(), true);
        form.Add(Note(client.Name));
        form.Add("Абонемент", choice);
        form.OnSubmit(() =>
        {
            int id = Selected<Membership>(choice, "абонемент").Id;
            return thaw ? repo.ThawAsync(id) : repo.CancelAsync(id);
        });
        return form;
    }
}
