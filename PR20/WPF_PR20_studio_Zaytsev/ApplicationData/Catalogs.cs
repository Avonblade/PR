using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace WPF_PR20_studio_Zaytsev.ApplicationData;

internal sealed partial class StudioRepository
{
    private const string PropColumns = """
        p.Id,p.Name,p.Category,p.Stock,p.Damaged,p.Rate,p.InRepair,
        COALESCE((SELECT SUM(l.Quantity) FROM dbo.Loans l WHERE l.PropId=p.Id AND l.ReturnedAt IS NULL),0),
        p.ImageId,p.Version
        """;

    public Task<List<Client>> ClientsAsync()
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        return Read(ReadClients);
    }

    public Task<List<Room>> RoomsAsync()
    {
        Require();
        return Read(ReadRooms);
    }

    public Task<List<Staff>> StaffAsync(bool photographersOnly = false)
    {
        if (photographersOnly) Require();
        else Require(StaffRole.Admin);
        return Read(db => ReadStaff(db,photographersOnly));
    }

    public Task<List<Prop>> PropsAsync()
    {
        Require();
        return Read(db => db.ReadAsync($"SELECT {PropColumns} FROM dbo.Props p ORDER BY p.Name;",ReadProp));
    }

    private static Task<List<Client>> ReadClients(StudioDatabase db) => db.ReadAsync("""
        SELECT c.Id,c.FullName,c.Phone,c.Email,c.Blocked,COALESCE(
            (SELECT AVG(t.Total) FROM dbo.Bookings b JOIN dbo.BookingTotals t ON t.Id=b.Id
             WHERE b.ClientId=c.Id AND b.Status=N'Completed'),0),c.Version
        FROM dbo.Clients c ORDER BY c.FullName,c.Id;
        """,r => new Client(r.GetInt32(0),r.GetString(1),r.GetString(2),Text(r,3),r.GetBoolean(4),
            r.GetDecimal(5),Version(r,6)));

    private static Task<List<Room>> ReadRooms(StudioDatabase db) => db.ReadAsync("""
        SELECT Id,Name,Area,Features,Capacity,HourlyRate,InRepair,Version FROM dbo.Rooms ORDER BY Name;
        """,r => new Room(r.GetInt32(0),r.GetString(1),r.GetDecimal(2),r.GetString(3),r.GetInt32(4),
            r.GetDecimal(5),r.GetBoolean(6),Version(r,7)));

    private static Task<List<Staff>> ReadStaff(StudioDatabase db,bool photographersOnly = false) => db.ReadAsync("""
        SELECT Id,FullName,Login,Role,Active,Version FROM dbo.Staff WHERE @photo=0 OR Role=N'Photo' ORDER BY FullName;
        """,r => new Staff(r.GetInt32(0),r.GetString(1),r.GetString(2),Code<StaffRole>(r,3),r.GetBoolean(4),Version(r,5)),
        ("@photo",photographersOnly));

    private static Prop ReadProp(SqlDataReader r) => new(r.GetInt32(0),r.GetString(1),r.GetString(2),r.GetInt32(3),
        r.GetInt32(4),r.GetDecimal(5),r.GetBoolean(6),r.GetInt32(7),Int(r,8),Version(r,9));

    public Task<int> SaveClientAsync(Client? original,string name,string phone,string? email,bool blocked)
    {
        Require(StaffRole.Admin,StaffRole.Manager);
        name = Required(name,100,"ФИО");
        phone = Required(phone,30,"Телефон");
        email = Optional(email,100,"Почта");
        return Write(async db =>
        {
            string sql = original is null ? """
                INSERT dbo.Clients(FullName,Phone,Email,Blocked) OUTPUT INSERTED.Id VALUES(@name,@phone,@email,@blocked);
                """ : """
                UPDATE dbo.Clients SET FullName=@name,Phone=@phone,Email=@email,Blocked=@blocked
                OUTPUT INSERTED.Id WHERE Id=@id AND Version=@version;
                """;
            int id = await db.ScalarAsync<int>(sql,("@name",name),("@phone",phone),("@email",email),("@blocked",blocked),
                ("@id",original?.Id),("@version",original?.Version));
            return id > 0 ? id : throw Stale();
        });
    }

    public Task SaveRoomAsync(Room? original,string name,decimal area,string features,int capacity,decimal rate,bool repair)
    {
        Require(StaffRole.Admin);
        name = Required(name,70,"Зал");
        features = Required(features,250,"Особенности");
        if (capacity <= 0 || area is <= 0 or > 999_999.99m || rate is <= 0 or > 999_999_999.99m)
            throw new StudioRuleException("Укажите положительные площадь, вместимость и тариф в допустимом диапазоне.");
        return Write(async db =>
        {
            if (repair && original is { Repair:false } && await db.ScalarAsync<int>("""
                SELECT TOP(1) 1 FROM dbo.Bookings WHERE RoomId=@id AND
                    (Status=N'InWork' OR (Status IN(N'Confirmed',N'Paid') AND EndsAt>@now));
                """,("@id",original.Id),("@now",Now())) != 0)
                throw new StudioRuleException("Перед ремонтом перенесите действующие бронирования зала.");
            string sql = original is null ? """
                INSERT dbo.Rooms(Name,Area,Features,Capacity,HourlyRate,InRepair)
                VALUES(@name,@area,@features,@capacity,@rate,@repair);
                """ : """
                UPDATE dbo.Rooms SET Name=@name,Area=@area,Features=@features,Capacity=@capacity,HourlyRate=@rate,
                    InRepair=@repair WHERE Id=@id AND Version=@version;
                """;
            if (await db.ExecuteAsync(sql,("@name",name),("@area",area),("@features",features),("@capacity",capacity),
                ("@rate",rate),("@repair",repair),("@id",original?.Id),("@version",original?.Version)) != 1) throw Stale();
        });
    }

    public async Task SaveStaffAsync(Staff? original,string name,string login,StaffRole role,bool active,string password)
    {
        var session = Require(StaffRole.Admin);
        name = Required(name,100,"ФИО");
        login = Required(login,50,"Логин");
        if (!Enum.IsDefined(role) || (original is null || password.Length > 0) && password.Length is < 8 or > 128)
            throw new StudioRuleException("Выберите роль. Новый пароль должен содержать 8–128 знаков.");
        if (original?.Id == session.Id && (!active || role != StaffRole.Admin))
            throw new StudioRuleException("Нельзя отключить собственную учётную запись или изменить её роль.");
        if (original is { Role:StaffRole.Photo } && role != StaffRole.Photo)
            throw new StudioRuleException("Для фотографа можно отключить доступ, сохранив его роль в истории съёмок.");
        byte[]? salt = password.Length == 0 ? null : RandomNumberGenerator.GetBytes(16);
        byte[]? hash = salt is null ? null : await Task.Run(() => Hash(password,salt));
        try
        {
            await Write(async db =>
            {
                if (original is { Role:StaffRole.Photo,Active:true } && !active &&
                    await db.ScalarAsync<int>("""
                        SELECT TOP(1) 1 FROM dbo.Bookings WHERE PhotographerId=@id AND
                         (Status=N'InWork' OR (Status IN(N'Confirmed',N'Paid') AND EndsAt>@now));
                        """,("@id",original.Id),("@now",Now())) != 0)
                    throw new StudioRuleException("Сначала переназначьте действующие съёмки фотографа.");
                string sql = original is null ? """
                    INSERT dbo.Staff(FullName,Login,Role,Active,Salt,Hash) VALUES(@name,@login,@role,@active,@salt,@hash);
                    """ : """
                    UPDATE dbo.Staff SET FullName=@name,Login=@login,Role=@role,Active=@active,
                        Salt=COALESCE(@salt,Salt),Hash=COALESCE(@hash,Hash) WHERE Id=@id AND Version=@version;
                    """;
                if (await db.ExecuteAsync(sql,("@name",name),("@login",login),("@role",role),("@active",active),
                    ("@salt",(object?)salt ?? System.Data.SqlTypes.SqlBinary.Null),
                    ("@hash",(object?)hash ?? System.Data.SqlTypes.SqlBinary.Null),
                    ("@id",original?.Id),("@version",original?.Version)) != 1) throw Stale();
            });
        }
        finally { if (hash is not null) CryptographicOperations.ZeroMemory(hash); }
    }

    public Task SavePropAsync(Prop? original,string name,string category,int stock,int damaged,decimal rate,bool repair,PhotoFile? image)
    {
        var session = Require(StaffRole.Admin,StaffRole.Store);
        if (!session.Admin)
        {
            if (original is null) throw new StudioRuleException("Добавлять позиции может администратор.");
            name = original.Name;
            category = original.Category;
            rate = original.Rate;
            image = null;
        }
        name = Required(name,100,"Название реквизита");
        if (!StudioText.Categories.Contains(category) || stock is < 0 or > 1_000_000 ||
            damaged < 0 || damaged > stock || rate is < 0 or > 999_999_999.99m)
            throw new StudioRuleException("Проверьте категорию, количество, число повреждённых единиц и тариф.");
        return Write(async db =>
        {
            int? imageId = image is null ? original?.ImageId : await SaveImage(db,image);
            int id;
            if (original is null)
            {
                id = await db.ScalarAsync<int>("""
                    INSERT dbo.Props(Name,Category,Stock,Damaged,Rate,InRepair,ImageId)
                    OUTPUT INSERTED.Id VALUES(@name,@category,@stock,@damaged,@rate,@repair,@image);
                    """,("@name",name),("@category",category),("@stock",stock),("@damaged",damaged),
                    ("@rate",rate),("@repair",repair),("@image",imageId));
            }
            else
            {
                id = original.Id;
                int open = await OnLoan(db,id);
                if (stock - damaged < open || repair && open > 0)
                    throw new StudioRuleException("Новый остаток или режим ремонта несовместимы с незакрытыми выдачами.");
                if (await db.ExecuteAsync("""
                    UPDATE dbo.Props SET Name=@name,Category=@category,Stock=@stock,Damaged=@damaged,Rate=@rate,
                        InRepair=@repair,ImageId=@image WHERE Id=@id AND Version=@version;
                    """,("@name",name),("@category",category),("@stock",stock),("@damaged",damaged),
                    ("@rate",rate),("@repair",repair),("@image",imageId),("@id",id),("@version",original.Version)) != 1)
                    throw Stale();
            }
            await SetRepairPeriod(db,id,repair || damaged > 0);
        });
    }

    private static Task SetRepairPeriod(StudioDatabase db,int id,bool open) => db.ExecuteAsync("""
        IF @open=1 AND NOT EXISTS(SELECT 1 FROM dbo.Repairs WHERE PropId=@id AND EndedAt IS NULL)
            INSERT dbo.Repairs(PropId,StartedAt) VALUES(@id,@now);
        IF @open=0 UPDATE dbo.Repairs SET EndedAt=@now WHERE PropId=@id AND EndedAt IS NULL;
        """,("@id",id),("@open",open),("@now",Now()));

    private static Task<int> OnLoan(StudioDatabase db,int id) => db.ScalarAsync<int>("""
        SELECT COALESCE(SUM(Quantity),0) FROM dbo.Loans WHERE PropId=@id AND ReturnedAt IS NULL;
        """,("@id",id));
}
