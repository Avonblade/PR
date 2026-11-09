using System.Windows;
using System.Windows.Controls;
using WPF_PR19_fitness_Zaytsev.ApplicationData;
using static WPF_PR19_fitness_Zaytsev.MainPage.DialogKit;

namespace WPF_PR19_fitness_Zaytsev.MainPage;

internal sealed class PlansWindow : ClubWindow
{
    private readonly DataGrid grid;
    public bool Changed { get; private set; }

    public PlansWindow(FitnessRepository repo, ClientRow client, List<Membership> plans)
        : base("Абонементы клиента", "ИСТОРИЯ", client.Name, 850, 480)
    {
        if (repo.Session!.Admin)
        {
            var cancel = Button("Отменить абонемент", async (_, _) =>
            {
                try
                {
                    var form = ClientDialogs.PlanAction(repo, client, plans, false);
                    form.Owner = this;
                    if (form.ShowDialog() != true) return;
                    Changed = true;
                    plans = await repo.PlansAsync(client.Client.Id);
                    grid!.ItemsSource = plans;
                }
                catch (Exception exception) { ErrorText.Text = UiMessages.ErrorText(exception); }
            }, "DangerButton");
            AddDocked(cancel, Dock.Top);
        }

        grid = Table(("Тип", "TypeText", 0, null), ("Начало", "StartsOn", 105, "dd.MM.yyyy"),
            ("Окончание", "EndsOn", 110, "dd.MM.yyyy"), ("Лимит", "Limit", 75, null),
            ("Остаток", "Remaining", 85, null), ("Статус", "StateText", 123, null),
            ("Дата отмены", "CancelledOn", 110, "dd.MM.yyyy"));
        grid.ItemsSource = plans;
        if (plans.Count == 0) AddDocked(Note("У клиента пока нет абонементов."), Dock.Top);
        AddDocked(ErrorText, Dock.Bottom);
        Layout.Children.Add(grid);
    }
}
