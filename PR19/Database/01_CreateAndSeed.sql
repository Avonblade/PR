USE master;
GO
IF DB_ID(N'PR19_Fitness') IS NULL
    EXEC(N'CREATE DATABASE PR19_Fitness');
GO
USE PR19_Fitness;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;
SET DATEFIRST 1;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') AND name IN
    (N'Employees', N'Clients', N'Memberships', N'Freezes', N'Rooms', N'Trainings', N'Bookings', N'Visits'))
    THROW 50001, N'Таблицы ПР19 уже существуют. Данные сохранены.', 1;

CREATE TABLE dbo.Employees (
    Id int IDENTITY PRIMARY KEY,
    FullName nvarchar(100) NOT NULL,
    Role nvarchar(10) NOT NULL CHECK (Role IN (N'Admin', N'Sales', N'Coach', N'Desk')),
    Login nvarchar(50) NOT NULL UNIQUE,
    PasswordSalt binary(16) NOT NULL,
    PasswordHash binary(32) NOT NULL,
    Phone nvarchar(25) NOT NULL,
    IsActive bit NOT NULL,
    Version rowversion NOT NULL
);
CREATE TABLE dbo.Clients (
    Id int IDENTITY PRIMARY KEY,
    FullName nvarchar(100) NOT NULL,
    BirthDate date NOT NULL,
    Phone nvarchar(25) NOT NULL,
    Email nvarchar(100) NULL,
    Card nvarchar(30) NULL,
    Status nvarchar(10) NOT NULL CHECK (Status IN (N'Active', N'Blocked', N'Leave')),
    Version rowversion NOT NULL
);
CREATE UNIQUE INDEX UX_Clients_Card ON dbo.Clients(Card) WHERE Card IS NOT NULL;

CREATE TABLE dbo.Memberships (
    Id int IDENTITY PRIMARY KEY,
    ClientId int NOT NULL REFERENCES dbo.Clients(Id),
    Type nvarchar(12) NOT NULL CHECK (Type IN (N'Single', N'Month', N'Quarter', N'Year')),
    StartsOn date NOT NULL,
    EndsOn date NOT NULL,
    VisitLimit int NULL CHECK (VisitLimit > 0),
    State nvarchar(10) NOT NULL CHECK (State IN (N'Active', N'Frozen', N'Cancelled')),
    CancelledOn date NULL,
    CHECK (StartsOn <= EndsOn),
    CHECK (Type <> N'Single' OR (VisitLimit IS NOT NULL AND VisitLimit = 1)),
    CHECK ((State = N'Cancelled' AND CancelledOn IS NOT NULL) OR (State <> N'Cancelled' AND CancelledOn IS NULL)),
    UNIQUE (Id, ClientId)
);
CREATE INDEX IX_Memberships_ClientDates ON dbo.Memberships(ClientId, StartsOn, EndsOn);

CREATE TABLE dbo.Freezes (
    Id int IDENTITY PRIMARY KEY,
    MembershipId int NOT NULL REFERENCES dbo.Memberships(Id),
    StartsOn date NOT NULL,
    EndsOn date NOT NULL,
    Reason nvarchar(10) NOT NULL CHECK (Reason IN (N'Holiday', N'Illness', N'Other')),
    ReleasedOn date NULL,
    CHECK (StartsOn <= EndsOn)
);
CREATE UNIQUE INDEX UX_Freezes_Open ON dbo.Freezes(MembershipId) WHERE ReleasedOn IS NULL;

CREATE TABLE dbo.Rooms (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(60) NOT NULL UNIQUE,
    Capacity int NOT NULL CHECK (Capacity > 0),
    Version rowversion NOT NULL
);
CREATE TABLE dbo.Trainings (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    RoomId int NOT NULL REFERENCES dbo.Rooms(Id),
    Type nvarchar(10) NOT NULL CHECK (Type IN (N'Strength', N'Cardio', N'Group', N'Yoga')),
    CoachId int NOT NULL REFERENCES dbo.Employees(Id),
    MaxParticipants int NOT NULL CHECK (MaxParticipants > 0),
    Weekday int NOT NULL CHECK (Weekday BETWEEN 1 AND 7),
    StartsAt time(0) NOT NULL,
    EndsAt time(0) NOT NULL,
    DurationMinutes AS DATEDIFF(MINUTE, StartsAt, EndsAt) PERSISTED,
    Version rowversion NOT NULL,
    CHECK (StartsAt < EndsAt AND DATEDIFF(MINUTE, StartsAt, EndsAt) > 0)
);
CREATE TABLE dbo.Bookings (
    Id int IDENTITY PRIMARY KEY,
    TrainingId int NOT NULL REFERENCES dbo.Trainings(Id),
    ClientId int NOT NULL REFERENCES dbo.Clients(Id),
    SessionOn date NOT NULL,
    MembershipId int NULL,
    IsGuest bit NOT NULL,
    Attendance nvarchar(10) NOT NULL CHECK (Attendance IN (N'Pending', N'Present', N'Absent')),
    UNIQUE (TrainingId, SessionOn, ClientId),
    UNIQUE (Id, ClientId),
    FOREIGN KEY (MembershipId, ClientId) REFERENCES dbo.Memberships(Id, ClientId),
    CHECK ((IsGuest = 1 AND MembershipId IS NULL) OR (IsGuest = 0 AND MembershipId IS NOT NULL))
);
CREATE TABLE dbo.Visits (
    Id int IDENTITY PRIMARY KEY,
    ClientId int NOT NULL REFERENCES dbo.Clients(Id),
    MembershipId int NULL,
    EnteredAt datetime2(0) NOT NULL,
    ExitedAt datetime2(0) NULL,
    AccessType nvarchar(12) NOT NULL CHECK (AccessType IN (N'Membership', N'Single', N'Guest', N'Group')),
    Price decimal(9,2) NULL,
    CoachId int NULL REFERENCES dbo.Employees(Id),
    BookingId int NULL,
    FOREIGN KEY (MembershipId, ClientId) REFERENCES dbo.Memberships(Id, ClientId),
    FOREIGN KEY (BookingId, ClientId) REFERENCES dbo.Bookings(Id, ClientId),
    CHECK (ExitedAt >= EnteredAt),
    CHECK ((AccessType = N'Single' AND Price IS NOT NULL AND Price > 0) OR (AccessType <> N'Single' AND Price IS NULL)),
    CHECK ((AccessType = N'Group' AND CoachId IS NOT NULL AND BookingId IS NOT NULL)
        OR (AccessType <> N'Group' AND CoachId IS NULL AND BookingId IS NULL)),
    CHECK (AccessType = N'Group' OR (AccessType = N'Membership' AND MembershipId IS NOT NULL)
        OR (AccessType IN (N'Single', N'Guest') AND MembershipId IS NULL))
);
CREATE UNIQUE INDEX UX_Visits_Booking ON dbo.Visits(BookingId) WHERE BookingId IS NOT NULL;
CREATE INDEX IX_Visits_Client ON dbo.Visits(ClientId, EnteredAt DESC);
CREATE INDEX IX_Visits_Membership ON dbo.Visits(MembershipId) WHERE MembershipId IS NOT NULL;

INSERT dbo.Employees (FullName, Role, Login, PasswordSalt, PasswordHash, Phone, IsActive) VALUES
(N'Дарья Соколова', N'Admin', N'admin', 0x40ca795ae4058c25773ff4b2e5f665be, 0x8f05d58eeb47b2ea3532dee94578d2bdfaa85eb86ab6b6e3487b61f575bd2f8f, N'+7 000 000-19-01', 1),
(N'Михаил Беляев', N'Sales', N'sales', 0xa2357ff8ae39fb8c9ea9334f60669443, 0x3cf6911a168e34f48bbb83296ce33e9b6d4b29f86ee598184b3f9bcf18c77b64, N'+7 000 000-19-02', 1),
(N'Анна Ветрова', N'Coach', N'coach', 0x7462be894a9be361c57d1e1e3a65fab9, 0x546c9fce9b2734dd13ed6d9ee2557e12887e2f6694fca0bd8304e3af463a37af, N'+7 000 000-19-03', 1),
(N'Денис Озеров', N'Desk', N'desk', 0x7d2a863fdf466210ff98e4cf61e305e5, 0x546a657f6d547f6653d1afff0753a2c2f5375474a548701709bb35cac3079626, N'+7 000 000-19-04', 1),
(N'Лев Громов', N'Coach', N'coach2', 0x84085b2ba8eff5fb55a8e582f03dacee, 0x3cbec9e7c51036ee26c079ad88917a680503bfc098d2883f38338a1baee12af7, N'+7 000 000-19-05', 1);

INSERT dbo.Clients (FullName, BirthDate, Phone, Email, Card, Status) VALUES
(N'Вера Климова', '19920414', N'+7 000 000-19-11', N'klimova@example.test', N'FIT-101', N'Active'),
(N'Глеб Степанов', '19880506', N'+7 000 000-19-12', N'stepanov@example.test', N'FIT-102', N'Active'),
(N'Елена Никитина', '19961122', N'+7 000 000-19-13', N'nikitina@example.test', N'FIT-103', N'Blocked'),
(N'Илья Котов', '20010209', N'+7 000 000-19-14', N'kotov@example.test', N'FIT-104', N'Active'),
(N'Лидия Смирнова', '19890913', N'+7 000 000-19-15', NULL, N'FIT-105', N'Leave'),
(N'Павел Миронов', '19950301', N'+7 000 000-19-16', NULL, N'FIT-106', N'Active'),
(N'София Романова', '19990118', N'+7 000 000-19-17', N'romanova@example.test', N'FIT-107', N'Active');

DECLARE @today date = CONVERT(date, SYSDATETIME());
DECLARE @weekday int = DATEPART(weekday, @today);

INSERT dbo.Memberships (ClientId, Type, StartsOn, EndsOn, VisitLimit, State) VALUES
(1, N'Month', DATEADD(DAY, -25, @today), DATEADD(DAY, 5, @today), 4, N'Active'),
(2, N'Year', DATEADD(DAY, -40, @today), DATEADD(DAY, 324, @today), NULL, N'Active'),
(3, N'Quarter', DATEADD(DAY, -60, @today), DATEADD(DAY, 29, @today), 12, N'Active'),
(4, N'Month', DATEADD(DAY, -9, @today), DATEADD(DAY, 20, @today), 1, N'Active'),
(5, N'Month', DATEADD(DAY, -20, @today), DATEADD(DAY, 12, @today), 8, N'Frozen'),
(6, N'Quarter', DATEADD(DAY, -94, @today), DATEADD(DAY, -5, @today), NULL, N'Active');

INSERT dbo.Freezes (MembershipId, StartsOn, EndsOn, Reason) VALUES
(5, DATEADD(DAY, 10, @today), DATEADD(DAY, 12, @today), N'Holiday');

INSERT dbo.Rooms (Name, Capacity) VALUES (N'Студия 1', 8), (N'Большой зал', 16), (N'Малая студия', 2);

INSERT dbo.Trainings (Name, RoomId, Type, CoachId, MaxParticipants, Weekday, StartsAt, EndsAt) VALUES
(N'Йога и баланс', 1, N'Yoga', 3, 8, @weekday, '08:00', '09:00'),
(N'Силовой круг', 2, N'Strength', 5, 12, @weekday, '17:30', '18:20'),
(N'Кардио-ритм', 2, N'Cardio', 5, 12, (@weekday % 7) + 1, '18:30', '19:15'),
(N'Мобильность', 3, N'Group', 3, 2, (@weekday % 7) + 1, '19:00', '19:45');

INSERT dbo.Bookings (TrainingId, ClientId, SessionOn, MembershipId, IsGuest, Attendance) VALUES
(1, 1, @today, 1, 0, N'Pending'),
(1, 2, @today, 2, 0, N'Pending'),
(4, 7, DATEADD(DAY, 1, @today), NULL, 1, N'Pending');

DECLARE @yesterday datetime2(0) = DATEADD(DAY, -1, CONVERT(datetime2(0), @today));
INSERT dbo.Visits (ClientId, MembershipId, EnteredAt, ExitedAt, AccessType, Price) VALUES
(1, 1, DATEADD(HOUR, 9, @yesterday), DATEADD(HOUR, 10, @yesterday), N'Membership', NULL),
(4, 4, DATEADD(HOUR, 11, @yesterday), DATEADD(HOUR, 12, @yesterday), N'Membership', NULL),
(2, 2, DATEADD(HOUR, -2, SYSDATETIME()), NULL, N'Membership', NULL),
(7, NULL, DATEADD(HOUR, 15, @yesterday), DATEADD(HOUR, 16, @yesterday), N'Single', 600);

COMMIT;
GO
