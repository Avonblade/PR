using System.IO;
using System.Data;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed partial class StudioRepository
{
    private const decimal MaxBookingTotal = 999_999_999_999.99m;
    public Session? Session { get; private set; }

    public async Task<bool> SignInAsync(string login, string password)
    {
        Session = null;
        if (login.Length is 0 or > 50 || password.Length is 0 or > 128) return false;
        var rows = await Read(db => db.ReadAsync("""
            SELECT Id,FullName,Role,Salt,Hash FROM dbo.Staff WHERE Login=@login AND Active=1;
            """, r => (new Session(r.GetInt32(0),r.GetString(1),Code<StaffRole>(r,2)),
                       r.GetFieldValue<byte[]>(3),r.GetFieldValue<byte[]>(4)), ("@login",login)));
        var (candidate,salt,hash) = rows.SingleOrDefault();
        byte[] check = await Task.Run(() => Hash(password,salt ?? new byte[16]));
        Session = CryptographicOperations.FixedTimeEquals(check,hash ?? new byte[32]) ? candidate : null;
        CryptographicOperations.ZeroMemory(check);
        return Session is not null;
    }

    public void SignOut() => Session = null;

    private static byte[] Hash(string password, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(password,salt,600_000,HashAlgorithmName.SHA256,32);

    private Session Require(params StaffRole[] roles)
    {
        if (Session is null || (roles.Length > 0 && !roles.Contains(Session.Role)))
            throw new StudioRuleException("Для этой операции недостаточно прав.");
        return Session;
    }

    private static void Own(Session session, Booking booking)
    {
        if (session.Photographer && session.Id != booking.PhotoId)
            throw new StudioRuleException("Фотографу доступны только его съёмки.");
    }

    private static async Task<T> Read<T>(Func<StudioDatabase,Task<T>> read)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        return await read(new StudioDatabase(connection));
    }

    private static async Task<T> Write<T>(Func<StudioDatabase,Task<T>> write)
    {
        await using var connection = await DatabaseConnection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);
        var db = new StudioDatabase(connection,transaction);
        await db.ExecuteAsync("""
            DECLARE @r int;
            EXEC @r=sys.sp_getapplock @Resource=N'PR20_Studio_Write', @LockMode=N'Exclusive',
                @LockOwner=N'Transaction', @LockTimeout=8000;
            IF @r < 0 THROW 50010,N'Данные заняты.',1;
            """);
        T result = await write(db);
        await transaction.CommitAsync();
        return result;
    }

    private static Task Write(Func<StudioDatabase,Task> write) =>
        Write(async db => { await write(db); return true; });

    private static T Code<T>(SqlDataReader r,int i) where T:struct,Enum => Enum.Parse<T>(r.GetString(i));
    private static string? Text(SqlDataReader r,int i) => r.IsDBNull(i) ? null : r.GetString(i);
    private static DateTime? Date(SqlDataReader r,int i) => r.IsDBNull(i) ? null : r.GetDateTime(i);
    private static int? Int(SqlDataReader r,int i) => r.IsDBNull(i) ? null : r.GetInt32(i);
    private static byte[] Version(SqlDataReader r,int i) => r.GetFieldValue<byte[]>(i);
    private static StudioRuleException Stale() => new("Данные изменились. Обновите список и повторите действие.");
    private static DateTime Now()
    {
        DateTime now = DateTime.Now;
        return now.AddTicks(-(now.Ticks % TimeSpan.TicksPerSecond));
    }
    private static string Required(string? value,int max,string label)
    {
        string clean = value?.Trim() ?? "";
        if (clean.Length is 0 || clean.Length > max)
            throw new StudioRuleException($"Заполните «{label}» (до {max} знаков).");
        return clean;
    }
    private static string? Optional(string? value,int max,string label) =>
        string.IsNullOrWhiteSpace(value) ? null : Required(value,max,label);

    private static async Task<int> SaveImage(StudioDatabase db,PhotoFile photo)
    {
        if (photo.Bytes.Length is 0 or > 10_485_760)
            throw new StudioRuleException("Размер фотографии должен быть от 1 байта до 10 МБ.");
        string name = Required(Path.GetFileName(photo.Name),200,"Имя фотографии");
        byte[] hash = SHA256.HashData(photo.Bytes);
        int id = await db.ScalarAsync<int>("SELECT Id FROM dbo.Images WHERE ContentHash=@hash;",("@hash",hash));
        return id != 0 ? id : await db.ScalarAsync<int>("""
            INSERT dbo.Images(Filename,ContentHash,Content) OUTPUT INSERTED.Id VALUES(@name,@hash,@data);
            """,("@name",name),("@hash",hash),("@data",photo.Bytes));
    }

    public Task<PhotoFile> ImageAsync(int id,int? bookingId = null)
    {
        var session = Require();
        return Read(async db =>
        {
            if (bookingId is int booking)
            {
                Own(session,await GetBooking(db,booking));
                if (await db.ScalarAsync<int>("""
                    SELECT TOP(1) 1 WHERE EXISTS(SELECT 1 FROM dbo.Loans WHERE BookingId=@booking AND
                        (BeforeImageId=@id OR AfterImageId=@id))
                    OR (@allowed=1 AND EXISTS(SELECT 1 FROM dbo.Materials WHERE BookingId=@booking AND ImageId=@id));
                    """,("@booking",booking),("@id",id),("@allowed",session.Role != StaffRole.Store)) == 0)
                    throw new StudioRuleException("Фотография не найдена в этой съёмке.");
            }
            else if (await db.ScalarAsync<int>("SELECT TOP(1) 1 FROM dbo.Props WHERE ImageId=@id;",("@id",id)) == 0)
                throw new StudioRuleException("Фотография реквизита не найдена.");
            return (await db.ReadAsync("SELECT Filename,Content FROM dbo.Images WHERE Id=@id;",
                r => new PhotoFile(r.GetString(0),r.GetFieldValue<byte[]>(1)),("@id",id))).SingleOrDefault() ?? throw Stale();
        });
    }
}
