using System.Globalization;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal enum StaffRole { Admin, Manager, Photo, Store }
internal enum BookingState { Draft, Confirmed, Paid, InWork, Completed, Cancelled }
internal enum MaterialKind { Preview, Original, Final }
internal enum MaterialState { Uploaded, Editing, Ready }
internal enum PaymentMethod { Cash, Card, Online }
internal enum ReportKind { Rooms, Props, Debt, Quality, Conflicts }

internal static class StudioText
{
    public static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    public static readonly string[] Types = ["Портрет", "Каталог", "Свадьба", "Предметная", "Другое"];
    public static readonly string[] Categories = ["Мебель", "Фоны", "Световое оборудование", "Аксессуары"];
    public static string RoleName(this StaffRole role) => role switch
    {
        StaffRole.Admin => "Администратор", StaffRole.Manager => "Менеджер",
        StaffRole.Photo => "Фотограф", _ => "Кладовщик"
    };
    public static string StateName(this BookingState state) => state switch
    {
        BookingState.Draft => "Черновик", BookingState.Confirmed => "Подтверждено",
        BookingState.Paid => "Оплачено", BookingState.InWork => "В работе",
        BookingState.Completed => "Завершено", _ => "Отменено"
    };
    public static string KindName(this MaterialKind kind) => kind switch
    {
        MaterialKind.Preview => "Превью", MaterialKind.Original => "Исходники", _ => "Готовые"
    };
    public static string MaterialName(this MaterialState state) => state switch
    {
        MaterialState.Uploaded => "Загружено", MaterialState.Editing => "На обработке", _ => "Готово"
    };
    public static string MethodName(this PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Наличные", PaymentMethod.Card => "Карта", _ => "Онлайн"
    };
    public static string ReportName(this ReportKind kind) => kind switch
    {
        ReportKind.Rooms => "Загрузка залов", ReportKind.Props => "Оборот реквизита",
        ReportKind.Debt => "Задолженность", ReportKind.Quality => "Работа фотографов", _ => "Конфликты и отмены"
    };
    public static string Money(decimal amount) => amount.ToString("N2", Russian) + " ₽";
    public static string Stamp(DateTime value) => value.ToString("dd.MM.yyyy HH:mm", Russian);
}

internal sealed record Session(int Id, string Name, StaffRole Role)
{
    public bool Admin => Role == StaffRole.Admin;
    public bool Office => Role is StaffRole.Admin or StaffRole.Manager;
    public bool Photographer => Role == StaffRole.Photo;
    public bool CanIssue => Office || Role == StaffRole.Store;
    public bool CanReturn => CanIssue || Photographer;
}

internal sealed record Client(int Id, string Name, string Phone, string? Email, bool Blocked, decimal Average, byte[] Version)
{
    public string Caption => $"{Name} · {Phone}";
    public string Status => Blocked ? "В чёрном списке" : "Активен";
    public bool Matches(string term) => Name.Contains(term, StringComparison.CurrentCultureIgnoreCase) ||
        Phone.Contains(term, StringComparison.CurrentCultureIgnoreCase);
}

internal sealed record Room(int Id, string Name, decimal Area, string Features, int Capacity, decimal Rate, bool Repair, byte[] Version)
{
    public string Caption => $"{Name} · {StudioText.Money(Rate)}/ч";
    public string Status => Repair ? "На ремонте" : "Активен";
}

internal sealed record Staff(int Id, string Name, string Login, StaffRole Role, bool Active, byte[] Version)
{
    public string RoleText => Role.RoleName();
    public string Status => Active ? "Активен" : "Отключён";
}

internal sealed record Prop(int Id, string Name, string Category, int Stock, int Damaged, decimal Rate,
    bool Repair, int Open, int? ImageId, byte[] Version)
{
    public int OnHand => Repair ? 0 : Stock - Damaged - Open;
    public string Status => Repair ? "На ремонте" : Open > 0 ? "На выдаче" : Damaged == Stock && Stock > 0 ? "Повреждён" : "В наличии";
}

internal sealed record Booking(int Id, int ClientId, string Client, int RoomId, string Room, int PhotoId, string Photographer,
    DateTime Start, DateTime End, string Type, BookingState State, decimal HourlyRate, decimal Total,
    decimal Paid, decimal Advance, string? Notes, DateTime? Requested, bool QualityIssue, bool Conflict,
    bool? PropsFree, byte[] Version)
{
    public string Number => $"ФС-{Id:0000}";
    public string Time => $"{Start:HH:mm} – {(Start.Date == End.Date ? End.ToString("HH:mm") : End.ToString("dd.MM HH:mm"))}";
    public decimal Hours => (decimal)(End - Start).TotalMinutes / 60;
    public decimal Balance => Total - Paid;
    public string Status => State.StateName();
    public int Priority => State == BookingState.InWork ? -1 : (int)State;
    public string PropStatus => PropsFree is null ? "Без реквизита" : PropsFree.Value ? "Свободно" : "Занято";
    public bool Matches(string term) => new[] { Number, Client, Room, Photographer, Type }
        .Any(text => text.Contains(term, StringComparison.CurrentCultureIgnoreCase));
}

internal sealed record BookingInput(int ClientId, int RoomId, int PhotoId, DateTime Start, int Minutes, string Type);
internal sealed record Slot(DateTime Start, int RoomId, string Room, int PhotoId, string Photographer)
{
    public string Caption => $"{StudioText.Stamp(Start)} · {Room} · {Photographer}";
}
internal sealed record SlotCheck(IReadOnlyList<string> Conflicts, IReadOnlyList<Slot> Alternatives, IReadOnlySet<int> FreePhotographers);
internal sealed record Rental(Prop Prop, int Reserved, decimal AppliedRate, int Free, int Issued)
{
    public decimal Cost => Reserved * AppliedRate;
}
internal sealed record Loan(int Id, string Prop, int Quantity, DateTime Issued, DateTime? Returned,
    int Before, int? After, int? Damaged, string? IssueNote, string? ReturnNote)
{
    public string Status => Returned is null ? "Выдано" : Damaged > 0 ? $"Повреждено: {Damaged}" : "Возвращено целым";
    public string Caption => $"{Prop} · {Quantity} шт. · {Status}";
}

internal sealed record Material(int Id, MaterialKind Kind, MaterialState State, int? ImageId,
    string Location, string? Comment, DateTime Uploaded, DateTime? Ready)
{
    public string KindText => Kind.KindName();
    public string StateText => State.MaterialName();
}
internal sealed record Payment(int Id, decimal Amount, PaymentMethod Method, string Receipt, DateTime Paid, bool Advance)
{
    public string MethodText => Method.MethodName();
    public string Purpose => Advance ? "Предоплата" : "Оплата";
}
internal sealed record PhotoFile(string Name, byte[] Bytes);
internal sealed class StudioRuleException(string message) : InvalidOperationException(message);
