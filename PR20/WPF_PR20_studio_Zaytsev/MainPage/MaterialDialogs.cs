using System.Windows;
using System.Windows.Controls;
using WPF_PR20_studio_Zaytsev.ApplicationData;
using static WPF_PR20_studio_Zaytsev.MainPage.DialogKit;
using static WPF_PR20_studio_Zaytsev.MainPage.StudioUi;

namespace WPF_PR20_studio_Zaytsev.MainPage;

internal static class MaterialDialogs
{
    public static TableWindow<Material> Materials(StudioRepository repo,Booking booking)
    {
        bool manager = repo.Session!.Role == StaffRole.Manager;
        var window = new TableWindow<Material>("Фотоматериалы",booking.Number,
            "От 3 до 5 разных превью для закрытия съёмки. Исходники и готовые материалы хранятся как пути или ссылки.",
            Table(("Тип","KindText",112,null),("Статус","StateText",124,null),("Файл или ссылка","Location",0,null),
                ("Комментарий","Comment",0,null),("Загружено","Uploaded",142,"dd.MM.yyyy HH:mm"),
                ("Готово","Ready",142,"dd.MM.yyyy HH:mm")),() => repo.MaterialsAsync(booking.Id),1070);
        if (booking.State is not (BookingState.Draft or BookingState.Cancelled))
            window.AddAction("Загрузить материал",_ => AddAsync(),false,null);
        if (booking.State != BookingState.Cancelled)
            window.AddAction("Изменить статус",EditAsync);
        window.AddAction("Просмотр или копирование",async material =>
        {
            if (material!.ImageId is int image)
            {
                ShowPhoto(window,await repo.ImageAsync(image,booking.Id));
                return;
            }
            var form = new FormWindow("Путь к материалу",booking.Number,"Выделите и скопируйте путь или ссылку.","Закрыть");
            form.Add("Путь или ссылка",Input(material.Location,1000,true));
            form.OnSubmit(() => Task.CompletedTask);
            form.ShowOwned(window);
        });
        return window;

        async Task AddAsync()
        {
            var form = new FormWindow("Загрузить материал",booking.Number,
                "Для превью выберите фотографию. Для остальных материалов укажите путь или ссылку.","Загрузить",660);
            var kind = manager ? Choices(new[] { MaterialKind.Preview },StudioText.KindName,MaterialKind.Preview,true) :
                EnumChoice<MaterialKind>(StudioText.KindName,MaterialKind.Preview);
            var state = EnumChoice<MaterialState>(StudioText.MaterialName,MaterialState.Uploaded);
            var photo = new PhotoPicker(form);
            var location = Input("",1000);
            var comment = Memo("",500);
            var localPicker = Button("Выбрать файл…",(_,_) =>
            {
                if (ChooseLocation(form) is { } path) location.Text = path;
            },"SecondaryButton");
            localPicker.HorizontalAlignment = HorizontalAlignment.Left;
            var preview = Field("Фото превью",photo);
            var other = new StackPanel();
            other.Children.Add(Field("Абсолютный путь или ссылка HTTP(S)",location));
            other.Children.Add(localPicker);
            form.Add("Тип",kind);
            form.Add("Статус",state);
            form.Add(preview);
            form.Add(other);
            form.Add("Комментарий",comment);
            kind.SelectionChanged += (_,_) => Update();
            Update();
            form.OnSubmit(() =>
            {
                var type = Selected<MaterialKind>(kind,"тип");
                return repo.AddMaterialAsync(booking.Id,type,Selected<MaterialState>(state,"статус"),
                    type == MaterialKind.Preview ? photo.RequiredPhoto() : null,location.Text,comment.Text);
            });
            if (form.ShowOwned(window) == true) await window.ReloadAsync();

            void Update()
            {
                bool isPreview = Selected<MaterialKind>(kind,"тип") == MaterialKind.Preview;
                preview.Visibility = isPreview ? Visibility.Visible : Visibility.Collapsed;
                other.Visibility = isPreview ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        async Task EditAsync(Material? item)
        {
            var material = item!;
            if (manager && material.Kind != MaterialKind.Preview)
                throw new StudioRuleException("Менеджер может редактировать только превью.");
            var form = new FormWindow("Статус материала",booking.Number,material.Location,"Сохранить");
            var state = EnumChoice<MaterialState>(StudioText.MaterialName,material.State);
            var comment = Memo(material.Comment,500);
            form.Add("Статус",state);
            form.Add("Комментарий",comment);
            form.OnSubmit(() => repo.UpdateMaterialAsync(booking.Id,material,Selected<MaterialState>(state,"статус"),comment.Text));
            if (form.ShowOwned(window) == true) await window.ReloadAsync();
        }
    }
}
