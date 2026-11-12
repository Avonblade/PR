using System.Windows;
using System.Windows.Controls;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;
using static WPF_PR20_studio_Zaytsev.MainPage.StudioUi;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal static class RentalDialogs
{
    public static TableWindow<Rental> Rentals(StudioRepository repo,Booking booking)
    {
        var session = repo.Session!;
        var window = new TableWindow<Rental>(session.Office ? "Подбор реквизита" : "Реквизит съёмки",booking.Number,
            $"{booking.Room} · {StudioText.Stamp(booking.Start)}. Свободные единицы рассчитаны на время брони.",
            Table(("Позиция","Prop.Name",0,null),("Категория","Prop.Category",195,null),("Свободно","Free",91,null),
                ("На складе","Prop.OnHand",93,null),("В брони","Reserved",83,null),("Выдано","Issued",77,null),
                ("Тариф, ₽","AppliedRate",98,"N2"),("В сумме, ₽","Cost",102,"N2")),() => repo.RentalsAsync(booking.Id),1060);
        if (session.Office && booking.State is BookingState.Draft or BookingState.Confirmed or BookingState.Paid)
            window.AddAction("Указать количество",Reserve,true,null);
        if (session.CanIssue && booking.State is BookingState.Confirmed or BookingState.Paid or BookingState.InWork)
            window.AddAction("Выдать реквизит",Issue);
        window.AddAction("Выдачи и возвраты",async _ =>
        {
            Loans(repo,booking).ShowOwned(window);
            await window.ReloadAsync();
        },false);
        window.AddAction("Фото позиции",async row =>
        {
            if (row!.Prop.ImageId is not int image) throw new StudioRuleException("Фото позиции ещё не загружено.");
            ShowPhoto(window,await repo.ImageAsync(image));
        });

        var filters = new WrapPanel { Margin = new Thickness(0,4,0,5) };
        var category = Choices(new[] { "Все категории" }.Concat(StudioText.Categories),s => s,"Все категории",true);
        var availability = Choices(new[] { "Любое", "Свободно", "Занято" },s => s,"Любое",true);
        var max = Input("",13);
        AddFilter("Категория",category,179);
        AddFilter("Наличие",availability,116);
        AddFilter("Тариф до, ₽",max,116);
        var error = Note("");
        filters.Children.Add(error);
        window.AddFilters(filters);
        category.SelectionChanged += (_,_) => Filter();
        availability.SelectionChanged += (_,_) => Filter();
        max.TextChanged += (_,_) => Filter();

        void AddFilter(string label,FrameworkElement element,double width)
        {
            var field = Field(label,element);
            field.Width = width;
            field.Margin = new Thickness(0,0,8,0);
            filters.Children.Add(field);
        }

        void Filter()
        {
            try
            {
                decimal? price = string.IsNullOrWhiteSpace(max.Text) ? null : Amount(max,"Тариф",999_999_999.99m,true);
                error.Text = "";
                window.Filter(item => (category.SelectedIndex <= 0 || item.Prop.Category == Selected<string>(category,"категорию")) &&
                    (availability.SelectedIndex <= 0 || (item.Free > 0) == (availability.SelectedIndex == 1)) &&
                    (price is null || item.AppliedRate <= price));
            }
            catch (StudioRuleException ex) { error.Text = ex.Message; }
        }

        return window;

        async Task Reserve(Rental? item)
        {
            var row = item!;
            var form = new FormWindow("Подбор реквизита к бронированию",booking.Number,
                $"{row.Prop.Name} · свободно {row.Free} шт. Ноль удаляет позицию из брони.","Сохранить");
            var quantity = Input(row.Reserved.ToString(),7);
            var total = Note("");
            form.Add("Количество",quantity);
            form.Add(total);
            quantity.TextChanged += (_,_) => UpdateCost();
            UpdateCost();
            form.OnSubmit(() => repo.ReserveAsync(booking.Id,row.Prop.Id,NonnegativeInt(quantity,"Количество")));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();

            void UpdateCost()
            {
                total.Text = int.TryParse(quantity.Text,out int value) && value >= 0
                    ? $"Стоимость позиции: {StudioText.Money(value * row.AppliedRate)}." : "Введите целое количество.";
            }
        }

        async Task Issue(Rental? item)
        {
            var row = item!;
            var form = new FormWindow("Выдать реквизит",booking.Number,
                $"{row.Prop.Name} · в брони {row.Reserved} шт., уже выдано {row.Issued} шт., на складе {row.Prop.OnHand} шт.",
                "Зафиксировать выдачу");
            var quantity = Input(Math.Max(1,Math.Min(row.Reserved - row.Issued,row.Prop.OnHand)).ToString(),7);
            var photo = new PhotoPicker(form);
            var note = Memo("",400);
            form.Add("Количество",quantity);
            form.Add("Фото до выдачи",photo);
            form.Add("Комментарий о состоянии",note);
            form.OnSubmit(() => repo.IssueAsync(booking.Id,row.Prop.Id,PositiveInt(quantity,"Количество"),
                photo.RequiredPhoto(),note.Text));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();
        }
    }

    private static TableWindow<Loan> Loans(StudioRepository repo,Booking booking)
    {
        var window = new TableWindow<Loan>("Выдачи и возвраты",booking.Number,"",
            Table(("Реквизит","Prop",0,null),("Шт.","Quantity",60,null),("Выдано","Issued",142,"dd.MM.yyyy HH:mm"),
                ("Возвращено","Returned",142,"dd.MM.yyyy HH:mm"),("Состояние","Status",170,null),
                ("Комментарий до","IssueNote",0,null),("Комментарий после","ReturnNote",0,null)),
            () => repo.LoansAsync(booking.Id),1090);
        if (repo.Session!.CanReturn)
            window.AddAction("Принять реквизит",Return,true,null);
        window.AddAction("Фото до",async loan => ShowPhoto(window,await repo.ImageAsync(loan!.Before,booking.Id)));
        window.AddAction("Фото после",async loan =>
        {
            if (loan!.After is not int id) throw new StudioRuleException("Реквизит ещё не возвращён.");
            ShowPhoto(window,await repo.ImageAsync(id,booking.Id));
        });
        return window;

        async Task Return(Loan? selected)
        {
            var loan = selected!;
            if (loan.Returned is not null) throw new StudioRuleException("Эта выдача уже закрыта.");
            var form = new FormWindow("Принять реквизит",booking.Number,$"{loan.Prop} · {loan.Quantity} шт.",
                "Зафиксировать возврат");
            var damaged = Input("0",7);
            var photo = new PhotoPicker(form);
            var note = Memo("",400);
            form.Add("Повреждено единиц (0 — всё целое)",damaged);
            form.Add("Фото после возврата",photo);
            form.Add("Описание повреждений или комментарий",note);
            form.OnSubmit(() => repo.ReturnAsync(booking.Id,loan.Id,NonnegativeInt(damaged,"Повреждено"),
                photo.RequiredPhoto(),note.Text));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();
        }
    }
}
