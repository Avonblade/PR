USE master;
GO
IF DB_ID(N'PR20_PhotoStudio') IS NULL
    EXEC(N'CREATE DATABASE PR20_PhotoStudio');
GO
USE PR20_PhotoStudio;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_WARNINGS ON;
SET ANSI_PADDING ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND name IN
    (N'Staff',N'Clients',N'Rooms',N'Images',N'Props',N'Repairs',N'Bookings',
     N'Reservations',N'Loans',N'Materials',N'Payments',N'Conflicts'))
    THROW 50001, N'Таблицы ПР20 уже существуют. Данные сохранены.', 1;

CREATE TABLE dbo.Staff (
    Id int IDENTITY PRIMARY KEY, FullName nvarchar(100) NOT NULL,
    Login nvarchar(50) NOT NULL UNIQUE,
    Role nvarchar(10) NOT NULL CHECK (Role IN (N'Admin',N'Manager',N'Photo',N'Store')),
    Salt binary(16) NOT NULL, Hash binary(32) NOT NULL, Active bit NOT NULL,
    Version rowversion NOT NULL
);
CREATE TABLE dbo.Clients (
    Id int IDENTITY PRIMARY KEY, FullName nvarchar(100) NOT NULL,
    Phone nvarchar(30) NOT NULL, Email nvarchar(100) NULL,
    Blocked bit NOT NULL, Version rowversion NOT NULL
);
CREATE TABLE dbo.Rooms (
    Id int IDENTITY PRIMARY KEY, Name nvarchar(70) NOT NULL UNIQUE,
    Area decimal(8,2) NOT NULL CHECK (Area > 0), Features nvarchar(250) NOT NULL,
    Capacity int NOT NULL CHECK (Capacity > 0),
    HourlyRate decimal(11,2) NOT NULL CHECK (HourlyRate > 0),
    InRepair bit NOT NULL, Version rowversion NOT NULL
);
CREATE TABLE dbo.Images (
    Id int IDENTITY PRIMARY KEY, Filename nvarchar(200) NOT NULL,
    ContentHash binary(32) NOT NULL UNIQUE, Content varbinary(max) NOT NULL,
    CHECK (DATALENGTH(Content) BETWEEN 1 AND 10485760)
);
CREATE TABLE dbo.Props (
    Id int IDENTITY PRIMARY KEY, Name nvarchar(100) NOT NULL UNIQUE,
    Category nvarchar(35) NOT NULL CHECK (Category IN (N'Мебель',N'Фоны',N'Световое оборудование',N'Аксессуары')),
    Stock int NOT NULL CHECK (Stock >= 0), Damaged int NOT NULL DEFAULT 0,
    Rate decimal(11,2) NOT NULL CHECK (Rate >= 0), InRepair bit NOT NULL,
    ImageId int NULL REFERENCES dbo.Images(Id), Version rowversion NOT NULL,
    CHECK (Damaged BETWEEN 0 AND Stock)
);
CREATE TABLE dbo.Repairs (
    Id int IDENTITY PRIMARY KEY, PropId int NOT NULL REFERENCES dbo.Props(Id),
    StartedAt datetime2(0) NOT NULL, EndedAt datetime2(0) NULL,
    CHECK (EndedAt >= StartedAt)
);
CREATE UNIQUE INDEX UX_Repairs_Open ON dbo.Repairs(PropId) WHERE EndedAt IS NULL;

CREATE TABLE dbo.Bookings (
    Id int IDENTITY PRIMARY KEY, ClientId int NOT NULL REFERENCES dbo.Clients(Id),
    RoomId int NOT NULL REFERENCES dbo.Rooms(Id), PhotographerId int NOT NULL REFERENCES dbo.Staff(Id),
    StartsAt datetime2(0) NOT NULL, EndsAt datetime2(0) NOT NULL,
    ShootType nvarchar(20) NOT NULL CHECK (ShootType IN (N'Портрет',N'Каталог',N'Свадьба',N'Предметная',N'Другое')),
    Status nvarchar(12) NOT NULL CHECK (Status IN (N'Draft',N'Confirmed',N'Paid',N'InWork',N'Completed',N'Cancelled')),
    HourlyRate decimal(11,2) NOT NULL CHECK (HourlyRate > 0),
    Notes nvarchar(1000) NULL, FinishRequestedAt datetime2(0) NULL,
    CompletedAt datetime2(0) NULL, CancelledAt datetime2(0) NULL, CancelReason nvarchar(250) NULL,
    DebtNote nvarchar(250) NULL, DueOn date NULL, QualityIssue bit NOT NULL DEFAULT 0,
    Version rowversion NOT NULL,
    CHECK (DATEDIFF(MINUTE,StartsAt,EndsAt) BETWEEN 60 AND 720
        AND DATEPART(MINUTE, StartsAt) IN (0,30) AND DATEPART(MINUTE, EndsAt) IN (0,30)
        AND DATEPART(SECOND, StartsAt) = 0 AND DATEPART(SECOND, EndsAt) = 0),
    CHECK ((Status = N'Completed' AND CompletedAt IS NOT NULL) OR (Status <> N'Completed' AND CompletedAt IS NULL)),
    CHECK ((Status = N'Cancelled' AND CancelledAt IS NOT NULL AND CancelReason IS NOT NULL)
        OR (Status <> N'Cancelled' AND CancelledAt IS NULL AND CancelReason IS NULL))
);
CREATE INDEX IX_Bookings_RoomTime ON dbo.Bookings(RoomId,StartsAt,EndsAt) INCLUDE (Status);
CREATE INDEX IX_Bookings_PhotoTime ON dbo.Bookings(PhotographerId,StartsAt,EndsAt) INCLUDE (Status);

CREATE TABLE dbo.Reservations (
    BookingId int NOT NULL REFERENCES dbo.Bookings(Id), PropId int NOT NULL REFERENCES dbo.Props(Id),
    Quantity int NOT NULL CHECK (Quantity > 0), Rate decimal(11,2) NOT NULL CHECK (Rate >= 0),
    PRIMARY KEY (BookingId, PropId)
);
CREATE INDEX IX_Reservations_Prop ON dbo.Reservations(PropId,BookingId);

CREATE TABLE dbo.Loans (
    Id int IDENTITY PRIMARY KEY, BookingId int NOT NULL, PropId int NOT NULL,
    Quantity int NOT NULL CHECK (Quantity > 0), IssuedAt datetime2(0) NOT NULL,
    BeforeImageId int NOT NULL REFERENCES dbo.Images(Id), IssueNote nvarchar(400) NULL,
    ReturnedAt datetime2(0) NULL, AfterImageId int NULL REFERENCES dbo.Images(Id),
    DamagedCount int NULL, ReturnNote nvarchar(400) NULL,
    FOREIGN KEY (BookingId,PropId) REFERENCES dbo.Reservations(BookingId,PropId),
    CHECK ((ReturnedAt IS NULL AND AfterImageId IS NULL AND DamagedCount IS NULL)
        OR (ReturnedAt IS NOT NULL AND ReturnedAt >= IssuedAt AND AfterImageId IS NOT NULL
        AND DamagedCount IS NOT NULL AND DamagedCount BETWEEN 0 AND Quantity))
);
CREATE INDEX IX_Loans_Prop ON dbo.Loans(PropId,ReturnedAt);

CREATE TABLE dbo.Materials (
    Id int IDENTITY PRIMARY KEY, BookingId int NOT NULL REFERENCES dbo.Bookings(Id),
    Kind nvarchar(10) NOT NULL CHECK (Kind IN (N'Preview',N'Original',N'Final')),
    Status nvarchar(10) NOT NULL CHECK (Status IN (N'Uploaded',N'Editing',N'Ready')),
    ImageId int NULL REFERENCES dbo.Images(Id), Location nvarchar(1000) NULL,
    Comment nvarchar(500) NULL, UploadedAt datetime2(0) NOT NULL, ReadyAt datetime2(0) NULL,
    CHECK ((Kind = N'Preview' AND ImageId IS NOT NULL AND Location IS NULL)
        OR (Kind <> N'Preview' AND ImageId IS NULL AND Location IS NOT NULL)),
    CHECK ((Status = N'Ready' AND ReadyAt IS NOT NULL) OR (Status <> N'Ready' AND ReadyAt IS NULL))
);
CREATE UNIQUE INDEX UX_Materials_Preview ON dbo.Materials(BookingId,ImageId) WHERE ImageId IS NOT NULL;

CREATE TABLE dbo.Payments (
    Id int IDENTITY PRIMARY KEY, BookingId int NOT NULL REFERENCES dbo.Bookings(Id),
    Amount decimal(12,2) NOT NULL CHECK (Amount > 0),
    Method nvarchar(10) NOT NULL CHECK (Method IN (N'Cash',N'Card',N'Online')),
    Receipt nvarchar(60) NOT NULL UNIQUE, PaidAt datetime2(0) NOT NULL,
    IsAdvance bit NOT NULL
);
CREATE TABLE dbo.Conflicts (
    Id int IDENTITY PRIMARY KEY, RoomId int NOT NULL REFERENCES dbo.Rooms(Id),
    PhotographerId int NOT NULL REFERENCES dbo.Staff(Id),
    RoomConflict bit NOT NULL, PhotoConflict bit NOT NULL, LoggedAt datetime2(0) NOT NULL,
    CHECK (RoomConflict = 1 OR PhotoConflict = 1)
);

EXEC(N'
CREATE FUNCTION dbo.PropFree(@prop int,@starts datetime2(0),@ends datetime2(0),@exclude int)
RETURNS TABLE AS RETURN
(
    WITH Busy AS (
        SELECT b.StartsAt,b.EndsAt,r.Quantity
        FROM dbo.Reservations r JOIN dbo.Bookings b ON b.Id=r.BookingId
        WHERE r.PropId=@prop AND b.Id<>@exclude AND b.Status IN (N''Confirmed'',N''Paid'',N''InWork'')
          AND b.StartsAt<@ends AND b.EndsAt>@starts
    ), Points AS (
        SELECT @starts AS AtTime
        UNION SELECT StartsAt FROM Busy WHERE StartsAt>@starts
    ), Loads AS (
        SELECT (SELECT SUM(CONVERT(bigint,Quantity)) FROM Busy
                WHERE StartsAt<=p.AtTime AND EndsAt>p.AtTime) AS Used
        FROM Points p
    )
    SELECT CONVERT(int,CASE WHEN p.InRepair=1 OR p.Stock-p.Damaged<=used.Peak THEN 0
                       ELSE p.Stock-p.Damaged-used.Peak END) AS Free
    FROM dbo.Props p CROSS APPLY (SELECT COALESCE(MAX(Used),0) AS Peak FROM Loads) used
    WHERE p.Id=@prop
);
');

EXEC(N'
CREATE VIEW dbo.BookingTotals AS
SELECT b.Id,
       CAST(b.HourlyRate * DATEDIFF(MINUTE,b.StartsAt,b.EndsAt) / 60 + COALESCE(r.Cost,0) AS decimal(14,2)) Total,
       COALESCE(p.Paid,0) Paid, COALESCE(p.Advance,0) Advance
FROM dbo.Bookings b
OUTER APPLY (SELECT SUM(x.Rate * x.Quantity) AS Cost FROM dbo.Reservations x WHERE x.BookingId=b.Id) r
OUTER APPLY (SELECT SUM(x.Amount) AS Paid, SUM(CASE WHEN x.IsAdvance=1 THEN x.Amount ELSE 0 END) AS Advance
             FROM dbo.Payments x WHERE x.BookingId=b.Id) p;
');

INSERT dbo.Staff (FullName,Login,Role,Salt,Hash,Active) VALUES
(N'Алина Соколова',N'admin',N'Admin',0x706710e53b9d494fb7ebd1939b1ef5e4,0x01cce51d742c9189e5a1b019bb5e379107591c76eef8c0fcd7309f482d4bfcb8,1),
(N'Михаил Лебедев',N'manager',N'Manager',0xbfba9b6513245b8ba0b07e35a9b4b79a,0xe44b9c2e3cb5c486203e4d6e233769611082feed118faa11901d1f877d21e411,1),
(N'Анна Ветрова',N'photo',N'Photo',0x9117290abb1e87d00f6ad7d1fad71982,0xb8039e40b6ee74f1fc3124522ac5da2128c93817e4a9702d79828d60ae4acbd6,1),
(N'Денис Орлов',N'store',N'Store',0x0e2596d4b6839fb83b4c3e86f69bacc7,0xab5d027c0a6b5a1a87a6abe271252ee1620ddeff6b7d9f2ad8afa5f72248db40,1),
(N'Лев Громов',N'photo2',N'Photo',0xf1c535d1b3e366b3fb8d332c8a0b1741,0xb162a4177e321a96039d4d887f3e4d872d856db94095292bdd7c8f064ef2b2ed,1);
INSERT dbo.Clients (FullName,Phone,Email,Blocked) VALUES
(N'Вера Климова',N'+7 000 000-20-01',N'vera@example.test',0),
(N'Егор Панов',N'+7 000 000-20-02',N'egor@example.test',0),
(N'Мария Миронова',N'+7 000 000-20-03',NULL,0),
(N'Илья Власов',N'+7 000 000-20-04',NULL,1);
INSERT dbo.Rooms (Name,Area,Features,Capacity,HourlyRate,InRepair) VALUES
(N'Белый лофт',70,N'Дневной свет, белые стены, два окна',15,2400,0),
(N'Циклорама',110,N'Бесшовный белый фон, импульсный свет',20,3200,0),
(N'Тёмная студия',45,N'Чёрный фон, постоянный свет',8,1800,1);
INSERT dbo.Props (Name,Category,Stock,Rate,InRepair) VALUES
(N'Деревянный стул',N'Мебель',4,400,0),
(N'Бежевый бумажный фон',N'Фоны',2,700,0),
(N'Софтбокс 90 см',N'Световое оборудование',3,600,0),
(N'Винтажное кресло',N'Мебель',1,900,1);
DECLARE @today datetime2(0)=CONVERT(date,SYSDATETIME());
INSERT dbo.Repairs (PropId,StartedAt) VALUES (4,DATEADD(DAY,-4,@today));
INSERT dbo.Bookings (ClientId,RoomId,PhotographerId,StartsAt,EndsAt,ShootType,Status,HourlyRate) VALUES
(1,1,3,DATEADD(HOUR,10,@today),DATEADD(HOUR,12,@today),N'Портрет',N'Confirmed',2400),
(2,1,5,DATEADD(HOUR,13,@today),DATEADD(HOUR,15,@today),N'Каталог',N'Paid',2400),
(3,2,3,DATEADD(HOUR,-15,@today),DATEADD(HOUR,-13,@today),N'Предметная',N'InWork',3200),
(2,1,5,DATEADD(HOUR,11,@today),DATEADD(HOUR,12,@today),N'Свадьба',N'Draft',2400),
(1,2,5,DATEADD(HOUR,11,@today),DATEADD(HOUR,13,@today),N'Портрет',N'Confirmed',3200);
INSERT dbo.Bookings (ClientId,RoomId,PhotographerId,StartsAt,EndsAt,ShootType,Status,HourlyRate,CancelledAt,CancelReason)
VALUES (1,2,3,DATEADD(HOUR,16,@today),DATEADD(HOUR,17,@today),N'Портрет',N'Cancelled',3200,@today,N'Клиент изменил дату');
INSERT dbo.Reservations (BookingId,PropId,Quantity,Rate) VALUES (1,1,2,400),(3,3,1,600);
INSERT dbo.Payments (BookingId,Amount,Method,Receipt,PaidAt,IsAdvance) VALUES
(1,2000,N'Card',N'ФС-0001',@today,1),(2,4800,N'Online',N'ФС-0002',@today,1);
COMMIT;
GO
