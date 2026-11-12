using System.Windows;
using System.Windows.Controls;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;
using static WPF_PR20_studio_Zaytsev.MainPage.StudioUi;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal sealed class BookingEditor : FormWindow
{
    public BookingEditor(StudioRepository repo,IReadOnlyList<Client> clients,IReadOnlyList<Room> rooms,
        IReadOnlyList<Staff> photos,Booking? original)
        : base(original is null ? "Новая бронь" : "Редактирование брони",original?.Number ?? "",
            "Выберите зал, дату, длительность и фотографа. Реквизит можно добавить после сохранения черновика.",
            original is null || original.State == BookingState.Draft ? "Подтвердить бронь" : "Сохранить",735)
    {
        bool historical = original?.State == BookingState.Completed;
        var usableRooms = rooms.Where(r => !r.Repair || historical && r.Id == original!.RoomId).ToList();
        var usablePhotos = photos.Where(p => p.Active || historical && p.Id == original!.PhotoId).ToList();
        var client = ClientChoice(clients,clients.FirstOrDefault(c => c.Id == original?.ClientId));
        var room = Choices(usableRooms,r => r.Caption,usableRooms.FirstOrDefault(r => r.Id == original?.RoomId),true);
        var photo = Choices(usablePhotos,p => p.Name,usablePhotos.FirstOrDefault(p => p.Id == original?.PhotoId),true);
        var type = Choices(StudioText.Types,value => value,original?.Type ?? StudioText.Types[0],true);
        DateTime now = DateTime.Now;
        long halfHour = TimeSpan.FromMinutes(30).Ticks;
        DateTime startAt = original?.Start ?? new DateTime((now.Ticks / halfHour + 1) * halfHour);
        var date = Input(DateText(startAt),10);
        var time = Input(startAt.ToString("HH:mm"),5);
        var hours = Input(Numeric(original?.Hours ?? 1),4);
        var ends = Input("",18,true);
        var price = Note("");
        var slots = new ComboBox { DisplayMemberPath = nameof(Slot.Caption) };
        var feedback = Note("");
        bool filteredPhotographers = false;
        var checks = Button("Проверить время",style:"SecondaryButton");
        checks.Click += async (_,_) => await CheckAsync();
        var apply = Button("Выбрать вариант",(_,_) => ApplySlot(),"SecondaryButton");

        Add("Клиент · поиск по ФИО или телефону",client);
        var addClient = Button("Новый клиент…",async (_,_) =>
        {
            try
            {
                var form = CatalogDialogs.ClientForm(repo,null,out var saved);
                if (form.ShowOwned(this) == true)
                {
                    var all = await repo.ClientsAsync();
                    client.SetItems(all,all.Single(c => c.Id == saved()));
                }
            }
            catch (Exception e) { UiMessages.ShowError(this,e); }
        },"SecondaryButton");
        addClient.HorizontalAlignment = HorizontalAlignment.Left;
        Add(addClient);
        Add(Pair(Field("Зал",room),Field("Фотограф",photo)));
        Add(Pair(Field("Дата (ДД.ММ.ГГГГ)",date),Field("Начало (ЧЧ:ММ)",time)));
        Add(Pair(Field("Часы (1–12, шаг 0,5)",hours),Field("Окончание",ends)));
        Add("Тип съёмки",type);
        Add(price);
        var availability = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0,0,0,7) };
        availability.Children.Add(checks);
        availability.Children.Add(apply);
        Add(availability);
        Add("Свободные варианты",slots);
        Add(feedback);

        foreach (var box in new[] { date,time,hours }) box.TextChanged += (_,_) => InvalidateCheck();
        room.SelectionChanged += (_,_) => InvalidateCheck();
        UpdateEstimate();

        OnSubmit(async () => await repo.SaveBookingAsync(GetInput(),original,true));
        if (original is null || original.State == BookingState.Draft)
            AddSubmit("Сохранить черновик",async () => await repo.SaveBookingAsync(GetInput(),original,false));

        BookingInput GetInput(bool requireClient = true,bool requirePhoto = true)
        {
            var selectedRoom = Selected<Room>(room,"зал");
            DateTime start = Date(date,"дату") + Time(time);
            if (start.Year is < 2000 or > 2099)
                throw new StudioRuleException("Укажите дату съёмки в диапазоне 2000–2099 годов.");
            decimal duration = Amount(hours,"Часы",12);
            if (duration < 1 || duration * 2 != decimal.Truncate(duration * 2))
                throw new StudioRuleException("Длительность от 1 до 12 часов с шагом 0,5 часа.");
            if (start.Minute % 30 != 0)
                throw new StudioRuleException("Время начала должно быть с шагом 30 минут.");
            if (requireClient && client.Value is null) throw new StudioRuleException("Выберите клиента.");
            var photographer = Selection<Staff>(photo);
            if (requirePhoto && photographer is null) throw new StudioRuleException("Выберите фотографа.");
            return new BookingInput(client.Value?.Id ?? 0,selectedRoom.Id,photographer?.Id ?? 0,start,
                (int)(duration * 60),Selected<string>(type,"тип съёмки"));
        }

        void UpdateEstimate()
        {
            try
            {
                var input = GetInput(false,false);
                var currentRoom = Selected<Room>(room,"зал");
                ends.Text = StudioText.Stamp(input.Start.AddMinutes(input.Minutes));
                decimal propCost = original is null ? 0 :
                    original.Total - Math.Round(original.HourlyRate * original.Hours,2,MidpointRounding.AwayFromZero);
                decimal rate = original?.RoomId == currentRoom.Id ? original.HourlyRate : currentRoom.Rate;
                price.Text = $"Предварительно: {StudioText.Money(Math.Round(rate * input.Minutes / 60 + propCost,2,MidpointRounding.AwayFromZero))}"
                    + $" · реквизит {StudioText.Money(propCost)}";
            }
            catch (StudioRuleException) { ends.Clear(); price.Text = "Укажите зал, дату, время и длительность для расчёта."; }
        }

        async Task CheckAsync()
        {
            Layout.IsEnabled = false;
            try
            {
                var input = GetInput(false,false);
                var result = await repo.CheckSlotAsync(input,original?.Id ?? 0);
                var freePhotos = usablePhotos.Where(p => result.FreePhotographers.Contains(p.Id)).ToList();
                photo.ItemsSource = freePhotos.Select(p => new Option<Staff>(p,p.Name)).ToList();
                photo.SelectedItem = photo.Items.Cast<Option<Staff>>().FirstOrDefault(p => p.Value.Id == input.PhotoId);
                filteredPhotographers = true;
                slots.ItemsSource = result.Alternatives;
                slots.SelectedIndex = result.Alternatives.Count == 0 ? -1 : 0;
                string free = string.Join(", ",freePhotos.Select(p => p.Name));
                feedback.Text = (result.Conflicts.Count > 0 ? string.Join("\n",result.Conflicts) + "\n" : "") +
                    (free.Length == 0 ? "Свободных фотографов на это время нет." : $"Свободные фотографы: {free}.");
            }
            catch (Exception ex) { feedback.Text = UiMessages.ErrorText(ex); }
            finally { Layout.IsEnabled = true; }
        }

        void InvalidateCheck()
        {
            if (filteredPhotographers)
            {
                int? id = Selection<Staff>(photo)?.Id;
                photo.ItemsSource = usablePhotos.Select(p => new Option<Staff>(p,p.Name)).ToList();
                photo.SelectedItem = photo.Items.Cast<Option<Staff>>().FirstOrDefault(o => o.Value.Id == id);
                filteredPhotographers = false;
            }
            slots.ItemsSource = null;
            feedback.Text = "";
            ErrorText.Text = "";
            UpdateEstimate();
        }

        void ApplySlot()
        {
            if (slots.SelectedItem is not Slot slot) return;
            date.Text = DateText(slot.Start);
            time.Text = slot.Start.ToString("HH:mm");
            room.SelectedItem = room.Items.Cast<Option<Room>>().FirstOrDefault(o => o.Value.Id == slot.RoomId);
            photo.ItemsSource = usablePhotos.Select(p => new Option<Staff>(p,p.Name)).ToList();
            photo.SelectedItem = photo.Items.Cast<Option<Staff>>().FirstOrDefault(o => o.Value.Id == slot.PhotoId);
            feedback.Text = "Вариант подставлен. При сохранении занятость будет проверена повторно.";
            slots.ItemsSource = null;
        }
    }
}
