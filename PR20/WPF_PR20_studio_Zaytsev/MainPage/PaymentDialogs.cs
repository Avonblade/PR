using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;
using static WPF_PR20_studio_Zaytsev.MainPage.StudioUi;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal static class PaymentDialogs
{
    public static TableWindow<Payment> Payments(StudioRepository repo,Booking booking)
    {
        var window = new TableWindow<Payment>("Оплата бронирования",booking.Number,booking.Client,
            Table(("Чек","Receipt",0,null),("Назначение","Purpose",136,null),("Сумма, ₽","Amount",128,"N2"),
                ("Способ","MethodText",120,null),("Дата","Paid",150,"dd.MM.yyyy HH:mm")),
            () => repo.PaymentsAsync(booking.Id),850);
        if (booking.State is not (BookingState.Draft or BookingState.Cancelled))
            window.AddAction("Принять оплату",_ => PayAsync(),false,null);
        window.AddAction("Сохранить квитанцию",SaveAsync);
        return window;

        async Task PayAsync()
        {
            var latest = await repo.BookingAsync(booking.Id);
            if (latest.Balance <= 0) throw new StudioRuleException("Бронь оплачена полностью.");
            var form = new FormWindow("Принять оплату",latest.Number,
                $"Итого {StudioText.Money(latest.Total)} · Оплачено {StudioText.Money(latest.Paid)}.","Записать платёж");
            var amount = Input(Numeric(latest.Balance),16);
            var method = EnumChoice<PaymentMethod>(StudioText.MethodName,PaymentMethod.Card);
            var receipt = Input("",60);
            var remainder = Note("");
            amount.TextChanged += (_,_) => Preview();
            Preview();
            form.Add("Сумма, ₽",amount);
            form.Add("Способ оплаты",method);
            form.Add("Номер чека",receipt);
            form.Add(remainder);
            form.OnSubmit(async () => await repo.PayAsync(latest.Id,Amount(amount,"Сумма",9_999_999_999.99m),
                Selected<PaymentMethod>(method,"способ оплаты"),receipt.Text));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();

            void Preview()
            {
                try
                {
                    decimal balance = latest.Balance - Amount(amount,"Сумма",9_999_999_999.99m);
                    remainder.Text = balance < 0 ? "Сумма превышает остаток." : $"Остаток после оплаты: {StudioText.Money(balance)}.";
                }
                catch (StudioRuleException ex) { remainder.Text = ex.Message; }
            }
        }

        async Task SaveAsync(Payment? item)
        {
            var payment = item!;
            var dialog = new SaveFileDialog
            {
                FileName = $"Квитанция-{booking.Id}-{payment.Id}.txt",
                Filter = "Текстовый файл (*.txt)|*.txt", AddExtension = true, DefaultExt = ".txt"
            };
            if (dialog.ShowDialog(window) != true) return;
            string document = $"""
                ФотоСтудия
                Квитанция по записи об оплате № {payment.Receipt}

                Бронь {booking.Number}
                Клиент {booking.Client}
                Зал {booking.Room}
                Дата съёмки {StudioText.Stamp(booking.Start)}
                Платёж {StudioText.Stamp(payment.Paid)}
                {payment.Purpose} · {payment.MethodText} · {StudioText.Money(payment.Amount)}

                Учебный документ о записи платежа.
                """;
            await File.WriteAllTextAsync(dialog.FileName,document);
        }
    }
}
