USE master;
GO
IF DB_ID(N'PR18_MedicalInsurance') IS NULL
    EXEC(N'CREATE DATABASE PR18_MedicalInsurance');
GO
USE PR18_MedicalInsurance;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;

IF EXISTS (
    SELECT 1 FROM sys.tables
    WHERE schema_id = SCHEMA_ID(N'dbo')
      AND name IN (N'Users', N'Clients', N'Clinics', N'Policies', N'ClinicContracts',
                   N'Claims', N'Referrals', N'PremiumPayments', N'ClinicPayments',
                   N'InspectorReports', N'GovernmentRecords')
)
    THROW 50001, N'Таблицы ПР18 уже существуют. Данные сохранены.', 1;

CREATE TABLE dbo.Users (
    Id int IDENTITY PRIMARY KEY,
    Login nvarchar(50) NOT NULL UNIQUE,
    FullName nvarchar(100) NOT NULL,
    Role nvarchar(12) NOT NULL CHECK (Role IN (N'Admin', N'Inspector', N'Account')),
    PasswordSalt binary(16) NOT NULL,
    PasswordHash binary(32) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1
);
CREATE TABLE dbo.Clients (
    Id int IDENTITY PRIMARY KEY,
    FullName nvarchar(100) NOT NULL,
    BirthDate date NOT NULL,
    Phone nvarchar(25) NOT NULL,
    Email nvarchar(100) NULL,
    Version rowversion NOT NULL
);
CREATE TABLE dbo.Clinics (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(100) NOT NULL,
    City nvarchar(60) NOT NULL,
    Phone nvarchar(25) NOT NULL,
    Version rowversion NOT NULL,
    UNIQUE (Name, City)
);
CREATE TABLE dbo.Policies (
    Id int IDENTITY PRIMARY KEY,
    Number nvarchar(30) NOT NULL UNIQUE,
    ClientId int NOT NULL REFERENCES dbo.Clients(Id),
    InspectorId int NOT NULL REFERENCES dbo.Users(Id),
    StartsOn date NOT NULL,
    EndsOn date NOT NULL,
    Premium decimal(12,2) NOT NULL CHECK (Premium > 0),
    Version rowversion NOT NULL,
    CHECK (StartsOn <= EndsOn)
);
CREATE TABLE dbo.ClinicContracts (
    Id int IDENTITY PRIMARY KEY,
    Number nvarchar(30) NOT NULL UNIQUE,
    ClinicId int NOT NULL REFERENCES dbo.Clinics(Id),
    StartsOn date NOT NULL,
    EndsOn date NOT NULL,
    Notes nvarchar(300) NULL,
    Version rowversion NOT NULL,
    CHECK (StartsOn <= EndsOn)
);
CREATE TABLE dbo.Claims (
    Id int IDENTITY PRIMARY KEY,
    Number nvarchar(30) NOT NULL UNIQUE,
    PolicyId int NOT NULL REFERENCES dbo.Policies(Id),
    ParentClaimId int NULL REFERENCES dbo.Claims(Id),
    FiledOn date NOT NULL,
    Subject nvarchar(200) NOT NULL,
    Status nvarchar(15) NOT NULL CHECK (Status IN (N'Подана', N'Принята', N'Закрыта', N'Отклонена')),
    Version rowversion NOT NULL,
    CHECK (ParentClaimId <> Id)
);
CREATE TABLE dbo.Referrals (
    Id int IDENTITY PRIMARY KEY,
    Number nvarchar(30) NOT NULL UNIQUE,
    ClaimId int NOT NULL REFERENCES dbo.Claims(Id),
    ClinicContractId int NOT NULL REFERENCES dbo.ClinicContracts(Id),
    ReferredOn date NOT NULL,
    Service nvarchar(150) NOT NULL,
    Cost decimal(12,2) NOT NULL CHECK (Cost > 0),
    Version rowversion NOT NULL
);
CREATE TABLE dbo.PremiumPayments (
    Id int IDENTITY PRIMARY KEY,
    PolicyId int NOT NULL REFERENCES dbo.Policies(Id),
    PaidOn date NOT NULL,
    Amount decimal(12,2) NOT NULL CHECK (Amount > 0),
    Reference nvarchar(50) NOT NULL UNIQUE,
    Verified bit NOT NULL,
    Version rowversion NOT NULL
);
CREATE TABLE dbo.ClinicPayments (
    Id int IDENTITY PRIMARY KEY,
    ReferralId int NOT NULL REFERENCES dbo.Referrals(Id),
    PaidOn date NOT NULL,
    Amount decimal(12,2) NOT NULL CHECK (Amount > 0),
    Reference nvarchar(50) NOT NULL UNIQUE,
    Version rowversion NOT NULL
);
CREATE TABLE dbo.InspectorReports (
    Id int IDENTITY PRIMARY KEY,
    Number nvarchar(30) NOT NULL UNIQUE,
    InspectorId int NOT NULL REFERENCES dbo.Users(Id),
    PeriodFrom date NOT NULL,
    PeriodTo date NOT NULL,
    SubmittedOn date NOT NULL,
    Notes nvarchar(300) NOT NULL,
    Version rowversion NOT NULL,
    CHECK (PeriodFrom <= PeriodTo AND PeriodTo <= SubmittedOn)
);
CREATE TABLE dbo.GovernmentRecords (
    Id int IDENTITY PRIMARY KEY,
    Number nvarchar(30) NOT NULL UNIQUE,
    Kind nvarchar(25) NOT NULL,
    Recipient nvarchar(100) NOT NULL,
    PeriodFrom date NOT NULL,
    PeriodTo date NOT NULL,
    SentOn date NOT NULL,
    Amount decimal(12,2) NULL,
    Notes nvarchar(300) NULL,
    Version rowversion NOT NULL,
    CHECK (PeriodFrom <= PeriodTo AND PeriodTo <= SentOn),
    CHECK ((Kind = N'Налоговое отчисление' AND Amount IS NOT NULL AND Amount > 0)
        OR (Kind = N'Статистический отчёт' AND Amount IS NULL))
);

CREATE INDEX IX_Claims_Policy ON dbo.Claims(PolicyId);
CREATE INDEX IX_Claims_Parent ON dbo.Claims(ParentClaimId);
CREATE INDEX IX_Referrals_Claim ON dbo.Referrals(ClaimId);
CREATE INDEX IX_PremiumPayments_Policy ON dbo.PremiumPayments(PolicyId, PaidOn) INCLUDE (Amount, Verified);
CREATE INDEX IX_ClinicPayments_Referral ON dbo.ClinicPayments(ReferralId) INCLUDE (Amount);

INSERT dbo.Users (Login, FullName, Role, PasswordSalt, PasswordHash) VALUES
(N'admin', N'Алёна Воронова', N'Admin', 0x83084f77c815008993cdac4ca2cbb771, 0x97b5d65ca0f74c2cccd8a69c3490b2f87ba3e59d517802ea18b659f892a5b0fe),
(N'inspector', N'Ирина Орлова', N'Inspector', 0xe94f518fe0d747d9976b1974282e2872, 0x44385eed106777e92fe018f1ff826fd223ad31723785f1e4179054fdd4b43a3c),
(N'accountant', N'Максим Серов', N'Account', 0xa4b24df14ce9e12e15d1c9da884809cb, 0x4c693cfb4c9d296841412b009ef62f8a4e9f22edbf75207d339f2076d91e1c0a);

INSERT dbo.Clients (FullName, BirthDate, Phone, Email) VALUES
(N'Алексей Морозов', '19900619', N'+7 000 000-18-01', N'morozov@example.test'),
(N'Мария Волкова', '19871207', N'+7 000 000-18-02', N'volkova@example.test'),
(N'Ольга Белова', '20010224', N'+7 000 000-18-03', NULL);
INSERT dbo.Clinics (Name, City, Phone) VALUES
(N'Клиника «Линия здоровья»', N'Владивосток', N'+7 000 000-18-11'),
(N'Центр «Маяк»', N'Владивосток', N'+7 000 000-18-12');
INSERT dbo.Policies (Number, ClientId, InspectorId, StartsOn, EndsOn, Premium) VALUES
(N'ДМС-26001', 1, 2, '20260101', '20261231', 36000),
(N'ДМС-26002', 2, 2, '20260101', '20261231', 32000),
(N'ДМС-26003', 3, 2, '20260401', '20270331', 28000);
INSERT dbo.ClinicContracts (Number, ClinicId, StartsOn, EndsOn, Notes) VALUES
(N'ЛУ-26-01', 1, '20260101', '20261231', N'Диагностика и амбулаторное лечение'),
(N'ЛУ-26-02', 2, '20260101', '20261231', N'Консультации и восстановительное лечение');
INSERT dbo.Claims (Number, PolicyId, ParentClaimId, FiledOn, Subject, Status) VALUES
(N'З-26001', 1, NULL, '20260210', N'Консультация и обследование', N'Закрыта'),
(N'З-26002', 2, NULL, '20260301', N'Амбулаторная диагностика', N'Принята'),
(N'З-26003', 1, 1, '20260305', N'Восстановительное лечение после осложнения', N'Принята');
INSERT dbo.Referrals (Number, ClaimId, ClinicContractId, ReferredOn, Service, Cost) VALUES
(N'Н-26001', 1, 1, '20260212', N'Консультация и обследование', 5500),
(N'Н-26002', 3, 2, '20260306', N'Курс восстановительного лечения', 12600);
INSERT dbo.PremiumPayments (PolicyId, PaidOn, Amount, Reference, Verified) VALUES
(1, '20260115', 36000, N'ВЗ-26001', 1),
(2, '20260202', 16000, N'ВЗ-26002', 1),
(2, '20260227', 16000, N'ВЗ-26003', 0);
INSERT dbo.ClinicPayments (ReferralId, PaidOn, Amount, Reference) VALUES
(1, '20260219', 5500, N'ОП-26001'),
(2, '20260312', 6000, N'ОП-26002');
INSERT dbo.InspectorReports (Number, InspectorId, PeriodFrom, PeriodTo, SubmittedOn, Notes) VALUES
(N'ИН-26001', 2, '20260201', '20260228', '20260303', N'За февраль оформлено одно направление.'),
(N'ИН-26002', 2, '20260301', '20260331', '20260403', N'Зарегистрированы первичная заявка и обращение по осложнению.');
INSERT dbo.GovernmentRecords (Number, Kind, Recipient, PeriodFrom, PeriodTo, SentOn, Amount, Notes) VALUES
(N'НАЛ-26001', N'Налоговое отчисление', N'Налоговый орган', '20260101', '20260331', '20260424', 8700, N'Перечисление за I квартал'),
(N'СТ-26001', N'Статистический отчёт', N'Орган государственной статистики', '20260101', '20260331', '20260410', NULL, N'Сводка по обращениям за I квартал');

EXEC(N'
CREATE VIEW dbo.PolicySummary AS
SELECT p.*, COALESCE(v.Total, 0) AS VerifiedAmount,
       CASE WHEN p.Premium > COALESCE(v.Total, 0) THEN p.Premium - COALESCE(v.Total, 0) ELSE 0 END AS Balance
FROM dbo.Policies p
OUTER APPLY (
    SELECT SUM(x.Amount) AS Total FROM dbo.PremiumPayments x WHERE x.PolicyId = p.Id AND x.Verified = 1
) v;
');

EXEC(N'
CREATE VIEW dbo.ReferralSummary AS
SELECT r.*, COALESCE(v.Total, 0) AS PaidAmount, r.Cost - COALESCE(v.Total, 0) AS Balance
FROM dbo.Referrals r
OUTER APPLY (
    SELECT SUM(x.Amount) AS Total FROM dbo.ClinicPayments x WHERE x.ReferralId = r.Id
) v;
');

EXEC(N'
CREATE VIEW dbo.WorkflowErrors AS
SELECT 10 AS Priority, N''Ответственным по договору или отчёту должен быть инспектор.'' AS Message
WHERE EXISTS (SELECT 1 FROM dbo.Policies p JOIN dbo.Users u ON u.Id = p.InspectorId WHERE u.Role <> N''Inspector'')
   OR EXISTS (SELECT 1 FROM dbo.InspectorReports r JOIN dbo.Users u ON u.Id = r.InspectorId WHERE u.Role <> N''Inspector'')
UNION ALL
SELECT 20, N''Дата первичной заявки должна входить в срок действия договора ДМС.''
WHERE EXISTS (
    SELECT 1 FROM dbo.Claims c JOIN dbo.Policies p ON p.Id = c.PolicyId
    WHERE c.ParentClaimId IS NULL AND (c.FiledOn < p.StartsOn OR c.FiledOn > p.EndsOn)
)
UNION ALL
SELECT 30, N''Осложнение должно относиться к первичной заявке по тому же договору и иметь дату не раньше первичной заявки.''
WHERE EXISTS (
    SELECT 1 FROM dbo.Claims c JOIN dbo.Claims parent ON parent.Id = c.ParentClaimId
    WHERE c.PolicyId <> parent.PolicyId OR parent.ParentClaimId IS NOT NULL OR c.FiledOn < parent.FiledOn
)
UNION ALL
SELECT 40, N''Направление оформляется по принятой или закрытой заявке, не раньше её даты и в срок действия договора с клиникой.''
WHERE EXISTS (
    SELECT 1 FROM dbo.Referrals r JOIN dbo.Claims c ON c.Id = r.ClaimId
    JOIN dbo.ClinicContracts k ON k.Id = r.ClinicContractId
    WHERE r.ReferredOn < c.FiledOn OR r.ReferredOn NOT BETWEEN k.StartsOn AND k.EndsOn
       OR c.Status NOT IN (N''Принята'', N''Закрыта'')
)
UNION ALL
SELECT 50, N''Дата оплаты клинике не может быть раньше даты направления.''
WHERE EXISTS (
    SELECT 1 FROM dbo.ClinicPayments x JOIN dbo.Referrals r ON r.Id = x.ReferralId WHERE x.PaidOn < r.ReferredOn
)
UNION ALL
SELECT 60, N''К дате перечисления клинике взносы по договору ДМС должны быть полностью оплачены и проверены бухгалтерией.''
WHERE EXISTS (
    SELECT 1 FROM dbo.ClinicPayments x JOIN dbo.Referrals r ON r.Id = x.ReferralId
    JOIN dbo.Claims c ON c.Id = r.ClaimId JOIN dbo.Policies p ON p.Id = c.PolicyId
    WHERE p.Premium > COALESCE((
        SELECT SUM(v.Amount) FROM dbo.PremiumPayments v
        WHERE v.PolicyId = p.Id AND v.Verified = 1 AND v.PaidOn <= x.PaidOn
    ), 0)
)
UNION ALL
SELECT 70, N''Сумма перечислений клинике превышает стоимость услуг по направлению.''
WHERE EXISTS (SELECT 1 FROM dbo.ReferralSummary WHERE Balance < 0);
');

IF EXISTS (SELECT 1 FROM dbo.WorkflowErrors)
    THROW 50002, N'Начальные данные нарушают правила страхового учёта.', 1;
COMMIT;
GO
