using System.Windows;
using System.Windows.Controls;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;
using static WPF_PR20_studio_Zaytsev.MainPage.StudioUi;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal sealed class ShootWindow : ClubWindow
{
    private readonly StudioRepository repo;
    private readonly Session session;
    private readonly TextBlock status = Note("");
    private readonly TextBlock price = Note("");
    private readonly TextBlock comments = Note("");
    private readonly WrapPanel actions = new();
    private Booking booking;

    public ShootWindow(StudioRepository repo,Booking booking)
        : base("Карточка съёмки",booking.Number,
            $"{booking.Client} · {booking.Room} · {StudioText.Stamp(booking.Start)} · {booking.Photographer}",790)
    {
        this.repo = repo;
        this.booking = booking;
        session = repo.Session!;
        var content = new StackPanel();
        status.FontWeight = FontWeights.SemiBold;
        content.Children.Add(status);
        if (session.Office) content.Children.Add(price);
        if (session.Role != StaffRole.Store) content.Children.Add(comments);
        content.Children.Add(actions);
        ErrorText.Margin = new Thickness(0,9,0,0);
        content.Children.Add(ErrorText);
        Layout.Children.Add(new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        Present();
    }

    private void Present()
    {
        status.Text = booking.Requested is not null && booking.State == BookingState.InWork
            ? $"{booking.Status} · завершение передано менеджеру" : booking.Status;
        price.Text = $"Сумма {StudioText.Money(booking.Total)} · Предоплата {StudioText.Money(booking.Advance)} · Остаток {StudioText.Money(booking.Balance)}";
        comments.Text = string.IsNullOrWhiteSpace(booking.Notes) ? "Комментарий к съёмке пока пуст." : booking.Notes;
        actions.Children.Clear();
        if (session.Office && booking.State == BookingState.Draft)
            Add("Подтвердить",async () => await repo.SaveBookingAsync(new BookingInput(booking.ClientId,booking.RoomId,
                booking.PhotoId,booking.Start,(int)(booking.End-booking.Start).TotalMinutes,booking.Type),booking,true),null);
        if (session.Role != StaffRole.Store && booking.State is BookingState.Confirmed or BookingState.Paid)
            Add("Начать съёмку",() => repo.StartAsync(booking.Id),null);
        if (session.Photographer && booking.State == BookingState.InWork && booking.Requested is null)
            Add("Передать на закрытие",() => repo.RequestFinishAsync(booking.Id),null);
        if (session.Office && booking.State == BookingState.InWork)
            Add("Закрыть съёмку",CloseAsync,null);
        if (session.Office && booking.State == BookingState.Completed)
            Add("Возврат или пересъёмка",QualityAsync);
        Add("Реквизит",() =>
        {
            RentalDialogs.Rentals(repo,booking).ShowOwned(this);
            return Task.CompletedTask;
        });
        if (session.Role != StaffRole.Store)
        {
            Add("Фотоматериалы",() =>
            {
                MaterialDialogs.Materials(repo,booking).ShowOwned(this);
                return Task.CompletedTask;
            });
            if (booking.State != BookingState.Cancelled && (booking.State != BookingState.Completed || session.Admin))
                Add("Комментарий",NotesAsync);
        }
        if (session.Office)
            Add("Оплата и чеки",() =>
            {
                PaymentDialogs.Payments(repo,booking).ShowOwned(this);
                return Task.CompletedTask;
            });
    }

    private void Add(string text,Func<Task> action,string? style = "SecondaryButton")
    {
        var button = Button(text,async (_,_) => await Run(action),style);
        button.Margin = new Thickness(0,0,7,7);
        actions.Children.Add(button);
    }

    private async Task Run(Func<Task> action)
    {
        Layout.IsEnabled = false;
        ErrorText.Text = "";
        try
        {
            await action();
            booking = await repo.BookingAsync(booking.Id);
            Present();
        }
        catch (Exception ex) { ErrorText.Text = UiMessages.ErrorText(ex); }
        finally { Layout.IsEnabled = true; }
    }

    private Task NotesAsync()
    {
        var form = new FormWindow("Комментарий к съёмке",booking.Number,"", "Сохранить");
        var note = Memo(booking.Notes,1000);
        form.Add("Комментарий",note);
        form.OnSubmit(() => repo.SaveNotesAsync(booking.Id,note.Text));
        form.ShowOwned(this);
        return Task.CompletedTask;
    }

    private Task CloseAsync()
    {
        var form = new FormWindow("Закрыть съёмку",booking.Number,
            $"Остаток {StudioText.Money(booking.Balance)}. Проверьте превью и фотографии возвращённого реквизита.",
            "Подтвердить завершение");
        var agreement = Memo("",250);
        var due = Input(DateText(DateTime.Today),10);
        var quality = new CheckBox { Content = "Потребовался возврат средств или пересъёмка", MinHeight = 30 };
        if (booking.Balance > 0)
        {
            form.Add("Согласование с клиентом при наличии долга",agreement);
            form.Add("Согласованный срок оплаты",due);
        }
        form.Add(quality);
        form.OnSubmit(() => repo.CloseAsync(booking.Id,agreement.Text,
            booking.Balance > 0 ? Date(due,"срок оплаты") : null,quality.IsChecked == true));
        form.ShowOwned(this);
        return Task.CompletedTask;
    }

    private Task QualityAsync()
    {
        var form = new FormWindow("Возврат или пересъёмка",booking.Number,"Отметка учитывается в отчёте по фотографам.","Сохранить");
        var issue = new CheckBox { Content = "Был возврат средств или пересъёмка", IsChecked = booking.QualityIssue, MinHeight = 30 };
        form.Add(issue);
        form.OnSubmit(() => repo.UpdateQualityAsync(booking.Id,issue.IsChecked == true));
        form.ShowOwned(this);
        return Task.CompletedTask;
    }
}
