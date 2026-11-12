using System.Windows;
using System.Windows.Controls;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;
using static WPF_PR20_studio_Zaytsev.MainPage.StudioUi;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal static class CatalogDialogs
{
    public static TableWindow<Client> Clients(StudioRepository repo)
    {
        var window = new TableWindow<Client>("Клиенты","","Средний чек рассчитан по завершённым съёмкам.",
            Table(("ФИО","Name",0,null),("Телефон","Phone",170,null),("Почта","Email",0,null),
                ("Статус","Status",145,null),("Средний чек, ₽","Average",143,"N2")),repo.ClientsAsync);
        var search = Input("",100);
        search.Width = 226;
        search.TextChanged += (_,_) => window.Filter(client => client.Matches(search.Text.Trim()));
        window.AddFilters(Field("Поиск по ФИО или телефону",search));
        window.AddAction("Добавить",_ => Edit(null),false,null);
        window.AddAction("Редактировать",Edit);
        window.AddAction("История бронирований",async client =>
        {
            var history = new TableWindow<Booking>("История бронирований",client!.Name,"",
                Table(("Бронь","Number",90,null),("Начало","Start",146,"dd.MM.yyyy HH:mm"),
                    ("Зал","Room",0,null),("Статус","Status",132,null),("Сумма, ₽","Total",115,"N2"),
                    ("Оплачено, ₽","Paid",120,"N2")),() => repo.BookingsAsync(client.Id));
            history.ShowOwned(window);
            await window.ReloadAsync();
        });
        return window;

        async Task Edit(Client? client)
        {
            if (ClientForm(repo,client,out _).ShowOwned(window) == true) await window.ReloadAsync();
        }
    }

    public static FormWindow ClientForm(StudioRepository repo,Client? client,out Func<int> saved)
    {
        int id = 0;
        saved = () => id;
        var form = new FormWindow(client is null ? "Новый клиент" : "Редактирование клиента","","", "Сохранить");
        var name = Input(client?.Name,100);
        var phone = Input(client?.Phone,30);
        var email = Input(client?.Email,100);
        var blocked = new CheckBox { Content = "В чёрном списке", IsChecked = client?.Blocked ?? false, MinHeight = 28 };
        form.Add("ФИО",name);
        form.Add("Телефон",phone);
        form.Add("Электронная почта",email);
        form.Add(blocked);
        form.OnSubmit(async () => id = await repo.SaveClientAsync(client,name.Text,phone.Text,email.Text,blocked.IsChecked == true));
        return form;
    }

    public static TableWindow<Room> Rooms(StudioRepository repo)
    {
        var window = new TableWindow<Room>("Залы","","",
            Table(("Зал","Name",0,null),("Площадь, м²","Area",110,"0.##"),("Особенности","Features",0,null),
                ("Вместимость","Capacity",115,null),("Тариф, ₽/ч","Rate",112,"N2"),("Статус","Status",120,null)),repo.RoomsAsync);
        if (repo.Session!.Admin)
        {
            window.AddAction("Добавить",_ => Edit(null),false,null);
            window.AddAction("Редактировать",Edit);
        }
        return window;

        async Task Edit(Room? room)
        {
            var form = new FormWindow(room is null ? "Добавить зал" : "Редактировать зал","","Тариф используется в новых бронях.", "Сохранить");
            var name = Input(room?.Name,70);
            var area = Input(room is null ? "" : Numeric(room.Area),10);
            var features = Memo(room?.Features,250);
            var capacity = Input(room?.Capacity.ToString(),10);
            var rate = Input(room is null ? "" : Numeric(room.Rate),13);
            var repair = new CheckBox { Content = "На ремонте", IsChecked = room?.Repair ?? false, MinHeight = 28 };
            form.Add("Название",name);
            form.Add(Pair(Field("Площадь, м²",area),Field("Вместимость",capacity)));
            form.Add("Особенности",features);
            form.Add("Стоимость часа, ₽",rate);
            form.Add(repair);
            form.OnSubmit(() => repo.SaveRoomAsync(room,name.Text,Amount(area,"Площадь",999_999.99m),features.Text,
                PositiveInt(capacity,"Вместимость"),Amount(rate,"Стоимость часа",999_999_999.99m),repair.IsChecked == true));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();
        }
    }

    public static TableWindow<Prop> Props(StudioRepository repo)
    {
        var session = repo.Session!;
        var window = new TableWindow<Prop>("Реквизит","","Остаток на складе учитывает выданные и повреждённые единицы.",
            Table(("Реквизит","Name",0,null),("Категория","Category",170,null),("Всего","Stock",67,null),
                ("На складе","OnHand",93,null),("Повреждено","Damaged",108,null),("Тариф, ₽","Rate",110,"N2"),
                ("Статус","Status",120,null)),repo.PropsAsync);
        if (session.Admin) window.AddAction("Добавить",_ => Edit(null),false,null);
        if (session.Admin || session.Role == StaffRole.Store)
            window.AddAction(session.Admin ? "Редактировать" : "Уточнить остаток",Edit);
        window.AddAction("Фото",async prop =>
        {
            if (prop!.ImageId is not int image) throw new StudioRuleException("У этой позиции ещё нет фотографии.");
            ShowPhoto(window,await repo.ImageAsync(image));
        });
        return window;

        async Task Edit(Prop? prop)
        {
            var form = new FormWindow(prop is null ? "Добавить реквизит" : "Редактировать реквизит","","", "Сохранить");
            var name = Input(prop?.Name,100,!session.Admin);
            var category = Choices(StudioText.Categories,value => value,prop?.Category ?? StudioText.Categories[0],true);
            category.IsEnabled = session.Admin;
            var stock = Input(prop?.Stock.ToString() ?? "0",7);
            var damaged = Input(prop?.Damaged.ToString() ?? "0",7);
            var rate = Input(prop is null ? "0" : Numeric(prop.Rate),13,!session.Admin);
            var repair = new CheckBox { Content = "Вся позиция на ремонте", IsChecked = prop?.Repair ?? false, MinHeight = 28 };
            var photo = new PhotoPicker(form);
            form.Add("Название",name);
            form.Add("Категория",category);
            form.Add(Pair(Field("Всего единиц",stock),Field("Повреждено",damaged)));
            form.Add("Тариф за съёмку, ₽",rate);
            form.Add(repair);
            if (session.Admin) form.Add("Фото позиции",photo);
            form.OnSubmit(() => repo.SavePropAsync(prop,name.Text,Selected<string>(category,"категорию"),
                NonnegativeInt(stock,"Всего единиц"),NonnegativeInt(damaged,"Повреждено"),
                Amount(rate,"Тариф",999_999_999.99m,true),repair.IsChecked == true,photo.File));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();
        }
    }

    public static TableWindow<Staff> Staff(StudioRepository repo)
    {
        var window = new TableWindow<Staff>("Сотрудники","","",
            Table(("ФИО","Name",0,null),("Логин","Login",180,null),("Роль","RoleText",180,null),
                ("Доступ","Status",140,null)),() => repo.StaffAsync());
        window.AddAction("Добавить",_ => Edit(null),false,null);
        window.AddAction("Редактировать",Edit);
        return window;

        async Task Edit(Staff? staff)
        {
            var form = new FormWindow(staff is null ? "Добавить сотрудника" : "Редактировать сотрудника","","",
                "Сохранить");
            var name = Input(staff?.Name,100);
            var login = Input(staff?.Login,50);
            var role = EnumChoice<StaffRole>(StudioText.RoleName,staff?.Role ?? StaffRole.Manager);
            var password = new PasswordBox { MaxLength = 128 };
            var active = new CheckBox { Content = "Доступ разрешён", IsChecked = staff?.Active ?? true, MinHeight = 28 };
            form.Add("ФИО",name);
            form.Add(Pair(Field("Логин",login),Field("Роль",role)));
            form.Add(staff is null ? "Пароль" : "Новый пароль (оставьте пустым, чтобы сохранить прежний)",password);
            form.Add(active);
            form.OnSubmit(() => repo.SaveStaffAsync(staff,name.Text,login.Text,Selected<StaffRole>(role,"роль"),
                active.IsChecked == true,password.Password));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();
        }
    }
}

internal sealed class ReportsWindow : ClubWindow
{
    public ReportsWindow(StudioRepository repo) : base("Отчёты","","",990,600)
    {
        var type = EnumChoice<ReportKind>(StudioText.ReportName,ReportKind.Rooms);
        var start = Input(DateText(DateTime.Today.AddDays(-30)),10);
        var end = Input(DateText(DateTime.Today.AddDays(14)),10);
        var grid = new DataGrid { AutoGenerateColumns = true, Margin = new Thickness(0,9,0,0) };
        grid.AutoGeneratingColumn += (_,args) =>
        {
            args.Column.Width = new DataGridLength(1,DataGridLengthUnitType.Star);
            args.Column.MinWidth = 112;
            if (args.Column is DataGridTextColumn column && column.Binding is System.Windows.Data.Binding binding)
            {
                binding.StringFormat = args.PropertyType == typeof(decimal) ? "N2" :
                    args.PropertyType == typeof(DateTime) ? "dd.MM.yyyy" : null;
                binding.TargetNullValue = "—";
            }
        };
        AddDocked(Field("Отчёт",type),Dock.Top);
        AddDocked(Pair(Field("С даты",start),Field("По дату",end)),Dock.Top);
        var refresh = Button("Сформировать",async (_,_) => await Reload());
        refresh.HorizontalAlignment = HorizontalAlignment.Left;
        AddDocked(refresh,Dock.Top);
        AddDocked(ErrorText,Dock.Bottom);
        Layout.Children.Add(grid);
        Loaded += async (_,_) => await Reload();

        async Task Reload()
        {
            Layout.IsEnabled = false;
            ErrorText.Text = "";
            try
            {
                var table = await repo.ReportAsync(Selected<ReportKind>(type,"отчёт"),Date(start,"начальную дату"),Date(end,"конечную дату"));
                grid.ItemsSource = table.DefaultView;
                if (table.Rows.Count == 0) ErrorText.Text = "За выбранный период записей нет.";
            }
            catch (Exception ex)
            {
                grid.ItemsSource = null;
                ErrorText.Text = UiMessages.ErrorText(ex);
            }
            finally { Layout.IsEnabled = true; }
        }
    }
}
