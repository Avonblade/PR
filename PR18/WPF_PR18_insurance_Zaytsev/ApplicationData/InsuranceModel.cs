namespace WPF_PR18_insurance_Zaytsev.ApplicationData;

internal enum StaffRole { Admin, Inspector, Account }

internal sealed record StaffSession(string Name, StaffRole Role)
{
    public string RoleName => Role switch
    {
        StaffRole.Admin => "Администратор",
        StaffRole.Inspector => "Инспектор",
        _ => "Бухгалтер"
    };
}

internal enum FieldKind { Text, Date, Money, Choice, Lookup, Flag }

internal sealed record ChoiceItem(object? Value, string Caption);

internal sealed record Field(string Name, string Label, FieldKind Kind = FieldKind.Text,
    bool Required = true, int Length = 0, string? Lookup = null, string[]? Options = null);

internal sealed record SummaryColumn(string Name, string Label);

internal sealed record Entity(string Table, string Title, string Description, StaffRole Editor,
    Field[] Fields, string? Source = null, SummaryColumn[]? Summary = null, bool FinanceOnly = false)
{
    public string ReadSource => Source ?? Table;
    public bool VisibleTo(StaffRole role) => !FinanceOnly || role != StaffRole.Inspector;
    public bool EditableBy(StaffRole role) => role == StaffRole.Admin || role == Editor;
}

internal sealed record RecordRow(int Id, byte[] Version, object?[] Values);
internal sealed record EditData(object?[] Values, IReadOnlyDictionary<string, List<ChoiceItem>> Lookups);

internal static class InsuranceModel
{
    private const string ClientNames = "SELECT Id, FullName AS Caption FROM dbo.Clients";
    private const string ClinicNames = "SELECT Id, CONCAT(Name, N' · ', City) AS Caption FROM dbo.Clinics";
    private const string InspectorNames = "SELECT Id, FullName AS Caption FROM dbo.Users WHERE Role = N'Inspector'";
    private const string PolicyNames = """
        SELECT p.Id, CONCAT(p.Number, N' · ', c.FullName) AS Caption
        FROM dbo.Policies p JOIN dbo.Clients c ON c.Id = p.ClientId
        """;
    private const string ContractNames = """
        SELECT k.Id, CONCAT(k.Number, N' · ', c.Name) AS Caption
        FROM dbo.ClinicContracts k JOIN dbo.Clinics c ON c.Id = k.ClinicId
        """;
    private const string ClaimNames = "SELECT Id, CONCAT(Number, N' · ', Subject) AS Caption FROM dbo.Claims";
    private const string PrimaryClaimNames = ClaimNames + " WHERE ParentClaimId IS NULL";
    private const string ReferralNames = "SELECT Id, CONCAT(Number, N' · ', Service) AS Caption FROM dbo.Referrals";

    public static IReadOnlyList<Entity> Sections { get; } = Array.AsReadOnly<Entity>(
    [
        new("Policies", "Договоры ДМС", "Договоры с клиентами, ответственные инспекторы и остаток по взносам.", StaffRole.Admin,
            [Text("Number", "Номер", 30), Lookup("ClientId", "Клиент", ClientNames),
             Lookup("InspectorId", "Инспектор", InspectorNames), Date("StartsOn", "Начало"),
             Date("EndsOn", "Окончание"), Money("Premium", "Взнос, ₽")],
            "PolicySummary", [new("VerifiedAmount", "Проверено, ₽"), new("Balance", "Остаток, ₽")]),
        new("Clients", "Клиенты", "Справочник застрахованных клиентов.", StaffRole.Admin,
            [Text("FullName", "ФИО", 100), Date("BirthDate", "Дата рождения"), Text("Phone", "Телефон", 25),
             Text("Email", "Электронная почта", 100, false)]),
        new("Clinics", "Клиники", "Справочник лечебных учреждений.", StaffRole.Admin,
            [Text("Name", "Наименование", 100), Text("City", "Город", 60), Text("Phone", "Телефон", 25)]),
        new("ClinicContracts", "Договоры с клиниками", "Действующие и архивные договоры с лечебными учреждениями.", StaffRole.Admin,
            [Text("Number", "Номер", 30), Lookup("ClinicId", "Клиника", ClinicNames),
             Date("StartsOn", "Начало"), Date("EndsOn", "Окончание"), Text("Notes", "Условия", 300, false)]),
        new("Claims", "Заявки", "Первичные обращения и осложнения. Для осложнения выберите первичную заявку.", StaffRole.Inspector,
            [Text("Number", "Номер", 30), Lookup("PolicyId", "Договор ДМС", PolicyNames),
             Date("FiledOn", "Дата заявки"), Text("Subject", "Причина обращения", 200),
             new("Status", "Статус", FieldKind.Choice, Length: 15, Options: ["Подана", "Принята", "Закрыта", "Отклонена"]),
             Lookup("ParentClaimId", "Первичная заявка", PrimaryClaimNames, false)]),
        new("Referrals", "Направления", "Назначенные услуги, договоры с клиниками и остаток к оплате.", StaffRole.Inspector,
            [Text("Number", "Номер", 30), Lookup("ClaimId", "Заявка", ClaimNames),
             Lookup("ClinicContractId", "Договор с клиникой", ContractNames), Date("ReferredOn", "Дата направления"),
             Text("Service", "Услуга", 150), Money("Cost", "Стоимость, ₽")],
            "ReferralSummary", [new("PaidAmount", "Перечислено, ₽"), new("Balance", "Остаток, ₽")]),
        new("PremiumPayments", "Взносы клиентов", "Поступления по договорам ДМС. Проверенный взнос учитывается в остатке по договору.",
            StaffRole.Account,
            [Text("Reference", "Документ", 50), Lookup("PolicyId", "Договор ДМС", PolicyNames),
             Date("PaidOn", "Дата оплаты"), Money("Amount", "Сумма, ₽"), new("Verified", "Проверен", FieldKind.Flag)],
            FinanceOnly: true),
        new("ClinicPayments", "Оплата клиникам", "Перечисления за услуги по направлениям, включая лечение осложнений.", StaffRole.Account,
            [Text("Reference", "Документ", 50), Lookup("ReferralId", "Направление", ReferralNames),
             Date("PaidOn", "Дата оплаты"), Money("Amount", "Сумма, ₽")], FinanceOnly: true),
        new("InspectorReports", "Отчёты инспекторов", "Отчёты о работе инспекторов, переданные в бухгалтерию.", StaffRole.Inspector,
            [Text("Number", "Номер", 30), Lookup("InspectorId", "Инспектор", InspectorNames),
             Date("PeriodFrom", "Начало периода"), Date("PeriodTo", "Конец периода"),
             Date("SubmittedOn", "Дата передачи"), Text("Notes", "Содержание", 300)]),
        new("GovernmentRecords", "Налоги и статистика", "Журнал налоговых перечислений и отправленных статистических отчётов.",
            StaffRole.Account,
            [Text("Number", "Номер", 30),
             new("Kind", "Вид записи", FieldKind.Choice, Length: 25, Options: ["Налоговое отчисление", "Статистический отчёт"]),
             Text("Recipient", "Получатель", 100), Date("PeriodFrom", "Начало периода"), Date("PeriodTo", "Конец периода"),
             Date("SentOn", "Дата отправки"), Money("Amount", "Сумма, ₽", false), Text("Notes", "Примечание", 300, false)],
            FinanceOnly: true)
    ]);

    private static Field Text(string name, string label, int length, bool required = true)
        => new(name, label, Required: required, Length: length);
    private static Field Date(string name, string label) => new(name, label, FieldKind.Date);
    private static Field Money(string name, string label, bool required = true) => new(name, label, FieldKind.Money, required);
    private static Field Lookup(string name, string label, string query, bool required = true)
        => new(name, label, FieldKind.Lookup, required, Lookup: query);
}
