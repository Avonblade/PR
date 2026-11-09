using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace WPF_PR19_fitness_Zaytsev.ApplicationData;

internal sealed partial class FitnessRepository
{
    private const int PasswordIterations = 600_000;
    public StaffSession? Session { get; private set; }

    public async Task<bool> SignInAsync(string login, string password)
    {
        Session = null;
        if (login.Length is 0 or > 50 || password.Length is 0 or > 128) return false;

        var rows = await ReadAsync(db => db.ReadAsync("""
            SELECT Id, FullName, Role, PasswordSalt, PasswordHash FROM dbo.Employees
            WHERE Login = @login AND IsActive = 1;
            """, reader => (new StaffSession(reader.GetInt32(0), reader.GetString(1), Code<StaffRole>(reader, 2)),
                            reader.GetFieldValue<byte[]>(3), reader.GetFieldValue<byte[]>(4)), ("@login", login)));
        var (session, salt, expected) = rows.SingleOrDefault();
        byte[] actual = await Task.Run(() => HashPassword(password, salt ?? new byte[16]));
        bool valid = CryptographicOperations.FixedTimeEquals(actual, expected ?? new byte[32]);
        CryptographicOperations.ZeroMemory(actual);
        Session = valid ? session : null;
        return Session is not null;
    }

    public void SignOut() => Session = null;

    private StaffSession Require(params StaffRole[] roles)
    {
        if (Session is null || (roles.Length > 0 && !roles.Contains(Session.Role)))
            throw new FitnessRuleException("Для этой операции недостаточно прав.");
        return Session;
    }

    private static async Task<T> ReadAsync<T>(Func<FitnessDatabase, Task<T>> read)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        return await read(new FitnessDatabase(connection));
    }

    private static async Task WriteAsync(Func<FitnessDatabase, Task> write)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var db = new FitnessDatabase(connection, transaction);
        await db.ExecuteAsync("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource = N'PR19_Fitness_Write',
                @LockMode = N'Exclusive', @LockOwner = N'Transaction', @LockTimeout = 5000;
            IF @result < 0 THROW 50010, N'Данные заняты. Повторите операцию.', 1;
            """);
        await write(db);
        await transaction.CommitAsync();
    }

    private static byte[] HashPassword(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password, salt, PasswordIterations, HashAlgorithmName.SHA256, 32);

    private static T Code<T>(SqlDataReader reader, int index) where T : struct, Enum
        => Enum.Parse<T>(reader.GetString(index));
    private static string? Text(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetString(index);
    private static DateTime? Date(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetDateTime(index);
    private static int? Number(SqlDataReader reader, int index) => reader.IsDBNull(index) ? null : reader.GetInt32(index);
    private static FitnessRuleException Stale() => new("Данные изменились. Обновите список и повторите действие.");

    private static string Required(string value, int length, string label)
    {
        value = value.Trim();
        if (value.Length == 0 || value.Length > length)
            throw new FitnessRuleException($"Заполните поле «{label}» (до {length} знаков).");
        return value;
    }

    private static string? Optional(string? value, int length, string label)
        => string.IsNullOrWhiteSpace(value) ? null : Required(value, length, label);
}
