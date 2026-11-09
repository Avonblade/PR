using System.Globalization;

namespace WPF_PR19_fitness_Zaytsev.ApplicationData;

internal enum StaffRole { Admin, Sales, Coach, Desk }
internal enum ClientStatus { Active, Blocked, Leave }
internal enum PlanType { Single, Month, Quarter, Year }
internal enum PlanState { Active, Expired, Frozen, Cancelled, Scheduled }
internal enum FreezeReason { Holiday, Illness, Other }
internal enum AccessType { Membership, Single, Guest, Group }
internal enum WorkoutType { Strength, Cardio, Group, Yoga }
internal enum Attendance { Pending, Present, Absent }

internal sealed record StaffSession(int Id, string Name, StaffRole Role)
{
    public bool Admin => Role == StaffRole.Admin;
    public bool CanSell => Role is StaffRole.Admin or StaffRole.Sales;
    public bool CanRegister => Role is StaffRole.Admin or StaffRole.Desk;
    public bool CanViewClients => Role != StaffRole.Coach;
    public bool CanMark => Role is StaffRole.Admin or StaffRole.Coach;
}

internal static class FitnessText
{
    public static readonly CultureInfo Russian = CultureInfo.GetCultureInfo("ru-RU");
    public static readonly string[] Weekdays = ["Понедельник", "Вторник", "Среда", "Четверг", "Пятница", "Суббота", "Воскресенье"];

    public static string RoleName(this StaffRole role) => role switch
    {
        StaffRole.Admin => "Администратор",
        StaffRole.Sales => "Менеджер по продажам",
        StaffRole.Coach => "Тренер",
        _ => "Администратор стойки"
    };

    public static string ClientStatusName(this ClientStatus status) => status switch
    {
        ClientStatus.Active => "Активен",
        ClientStatus.Blocked => "Заблокирован",
        _ => "В отпуске"
    };

    public static string PlanTypeName(this PlanType type) => type switch
    {
        PlanType.Single => "Разовый",
        PlanType.Month => "Месячный",
        PlanType.Quarter => "Квартальный",
        _ => "Годовой"
    };

    public static string PlanStateName(this PlanState state) => state switch
    {
        PlanState.Active => "Активен",
        PlanState.Expired => "Истёк",
        PlanState.Frozen => "Заморожен",
        PlanState.Scheduled => "Ожидает начала",
        _ => "Отменён"
    };

    public static string FreezeReasonName(this FreezeReason reason) => reason switch
    {
        FreezeReason.Holiday => "Отпуск",
        FreezeReason.Illness => "Болезнь",
        _ => "Другое"
    };

    public static string AccessName(this AccessType type) => type switch
    {
        AccessType.Membership => "По абонементу",
        AccessType.Single => "Разовый вход",
        AccessType.Guest => "Гостевой",
        _ => "Групповая тренировка"
    };

    public static string WorkoutName(this WorkoutType type) => type switch
    {
        WorkoutType.Strength => "Силовая",
        WorkoutType.Cardio => "Кардио",
        WorkoutType.Group => "Групповая",
        _ => "Йога"
    };

    public static string AttendanceName(this Attendance state) => state switch
    {
        Attendance.Present => "Пришёл",
        Attendance.Absent => "Не пришёл",
        _ => "Ожидает отметки"
    };

    public static int WeekdayOf(DateTime date) => ((int)date.DayOfWeek + 6) % 7 + 1;
    public static string Time(TimeSpan time) => time.ToString(@"hh\:mm");
}

internal sealed record ClientProfile(int Id, string Name, DateTime BirthDate, string Phone,
    string? Email, string? Card, ClientStatus Status, byte[] Version)
{
    public string Caption => $"{Name} · {Phone}";
    public string StatusText => Status.ClientStatusName();
    public bool Matches(string text) =>
        Name.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
        Phone.Contains(text, StringComparison.CurrentCultureIgnoreCase) ||
        (Card?.Contains(text, StringComparison.CurrentCultureIgnoreCase) ?? false);
}

internal sealed record Membership(int Id, int ClientId, PlanType Type, DateTime StartsOn, DateTime EndsOn,
    int? Limit, int Used, PlanState StoredState, DateTime? CancelledOn)
{
    public int? Remaining => Limit is int limit ? Math.Max(0, limit - Used) : null;
    public PlanState StateOn(DateTime date) => StoredState == PlanState.Cancelled ? PlanState.Cancelled :
        EndsOn < date.Date ? PlanState.Expired : StoredState == PlanState.Frozen ? PlanState.Frozen :
        StartsOn > date.Date ? PlanState.Scheduled : PlanState.Active;
    public string TypeText => Type.PlanTypeName();
    public string StateText => StateOn(DateTime.Today).PlanStateName();
    public string Caption => $"№ {Id} · {TypeText} · {StartsOn:dd.MM.yyyy} – {EndsOn:dd.MM.yyyy} · {StateText}";
}

internal sealed record ClientRow(ClientProfile Client, Membership? Plan, DateTime? LastVisit)
{
    public string Name => Client.Name;
    public string Phone => Client.Phone;
    public string StatusText => Client.StatusText;
    public string PlanTypeText => Plan?.TypeText ?? "—";
    public string PlanStateText => Plan?.StateText ?? "—";
    public DateTime? EndsOn => Plan?.EndsOn;
    public int? Remaining => Plan?.Remaining;
    public string RemainingText => Plan is null ? "—" : Remaining?.ToString() ?? "Без лимита";
    public bool IsBlocked => Client.Status == ClientStatus.Blocked;
    public bool LowBalance => Plan?.StateOn(DateTime.Today) == PlanState.Active && Remaining is <= 3;
    public bool Expiring => Plan?.StateOn(DateTime.Today) == PlanState.Active && EndsOn <= DateTime.Today.AddDays(7);
}

internal sealed record VisitRow(int Id, int ClientId, string Client, DateTime EnteredAt, DateTime? ExitedAt,
    AccessType Access, decimal? Price, string? Coach)
{
    public string AccessText => Access.AccessName();
    public TimeSpan? Duration => ExitedAt - EnteredAt;
    public string DurationText => Duration is TimeSpan value
        ? $"{(int)value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}" : "В клубе";
    public string Caption => $"{Client} · {EnteredAt:dd.MM.yyyy HH:mm:ss} · {AccessText}";
}

internal sealed record Room(int Id, string Name, int Capacity, byte[] Version);
internal sealed record Employee(int Id, string Name, StaffRole Role, string Login, string Phone, bool IsActive, byte[] Version)
{
    public string RoleText => Role.RoleName();
    public string ActiveText => IsActive ? "Да" : "Нет";
}

internal sealed record Training(int Id, string Name, int RoomId, string RoomName, WorkoutType Type,
    int CoachId, string Coach, int MaxParticipants, int RoomCapacity, int Weekday, TimeSpan StartsAt,
    TimeSpan EndsAt, byte[] Version)
{
    public int Capacity => Math.Min(MaxParticipants, RoomCapacity);
    public int DurationMinutes => (int)(EndsAt - StartsAt).TotalMinutes;
    public string WeekdayText => FitnessText.Weekdays[Weekday - 1];
    public string TimeText => $"{FitnessText.Time(StartsAt)}–{FitnessText.Time(EndsAt)}";
    public string TypeText => Type.WorkoutName();
    public string Caption => $"{Name} · {WeekdayText}, {TimeText}";
}

internal sealed record ScheduleRow(Training Training, DateTime SessionOn, int Booked)
{
    public string Name => Training.Name;
    public string Room => Training.RoomName;
    public string Type => Training.TypeText;
    public string Coach => Training.Coach;
    public int Weekday => Training.Weekday;
    public string WeekdayText => Training.WeekdayText;
    public string Time => Training.TimeText;
    public int Duration => Training.DurationMinutes;
    public int Capacity => Training.Capacity;
}

internal sealed record BookingRow(int Id, string Client, bool IsGuest, Attendance State)
{
    public string AccessText => IsGuest ? "Гостевой" : "По абонементу";
    public string AttendanceText => State.AttendanceName();
}

internal sealed record ClubReport(int Clients, int ActivePlans, int Visits, int GroupVisits, int Guests, decimal SingleRevenue);
internal sealed class FitnessRuleException(string message) : InvalidOperationException(message);
