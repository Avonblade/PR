-- Практическая работа 16. Вариант 4: куртки, джемперы, жакеты.
-- Проект WPF_PR16_clothes_ZaytsevVI. SQL Server 2016+ / Express.
-- Запуск: sqlcmd -S ".\SQLEXPRESS" -E -C -b -f 65001 -i Database\01_CreateAndSeed.sql
USE master;
GO
IF DB_ID(N'PR16_AlisaClothes') IS NULL CREATE DATABASE PR16_AlisaClothes;
GO
USE PR16_AlisaClothes;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.ClothingProduct', N'U') IS NOT NULL
        THROW 50001, N'База уже содержит товары. Повторный импорт отменён; существующие данные сохранены.', 1;
    CREATE TABLE dbo.ClothingCategory
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_ClothingCategory PRIMARY KEY,
        Name nvarchar(60) NOT NULL CONSTRAINT UQ_ClothingCategory_Name UNIQUE,
        CONSTRAINT CK_ClothingCategory_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
    CREATE TABLE dbo.ClothingMaterial
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_ClothingMaterial PRIMARY KEY,
        Name nvarchar(60) NOT NULL CONSTRAINT UQ_ClothingMaterial_Name UNIQUE,
        CONSTRAINT CK_ClothingMaterial_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
    CREATE TABLE dbo.ClothingSize
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_ClothingSize PRIMARY KEY,
        Name nvarchar(60) NOT NULL CONSTRAINT UQ_ClothingSize_Name UNIQUE,
        CONSTRAINT CK_ClothingSize_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
    CREATE TABLE dbo.ClothingColor
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_ClothingColor PRIMARY KEY,
        Name nvarchar(60) NOT NULL CONSTRAINT UQ_ClothingColor_Name UNIQUE,
        CONSTRAINT CK_ClothingColor_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
    CREATE TABLE dbo.ClothingProduct
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_ClothingProduct PRIMARY KEY,
        Article nvarchar(30) NOT NULL CONSTRAINT UQ_ClothingProduct_Article UNIQUE,
        Name nvarchar(120) NOT NULL,
        CategoryId int NOT NULL CONSTRAINT FK_ClothingProduct_Category REFERENCES dbo.ClothingCategory(Id),
        MaterialId int NOT NULL CONSTRAINT FK_ClothingProduct_Material REFERENCES dbo.ClothingMaterial(Id),
        SizeId int NOT NULL CONSTRAINT FK_ClothingProduct_Size REFERENCES dbo.ClothingSize(Id),
        ColorId int NOT NULL CONSTRAINT FK_ClothingProduct_Color REFERENCES dbo.ClothingColor(Id),
        Price decimal(12,2) NOT NULL,
        Stock int NOT NULL,
        Description nvarchar(2000) NOT NULL CONSTRAINT DF_ClothingProduct_Description DEFAULT N'',
        Version rowversion NOT NULL,
        CONSTRAINT CK_ClothingProduct_Article CHECK (LEN(LTRIM(RTRIM(Article))) > 0),
        CONSTRAINT CK_ClothingProduct_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_ClothingProduct_Price CHECK (Price BETWEEN 0 AND 9999999999.99),
        CONSTRAINT CK_ClothingProduct_Stock CHECK (Stock BETWEEN 0 AND 1000000)
    );
    CREATE INDEX IX_ClothingProduct_CategoryId ON dbo.ClothingProduct(CategoryId);
    CREATE INDEX IX_ClothingProduct_MaterialId ON dbo.ClothingProduct(MaterialId);
    CREATE INDEX IX_ClothingProduct_SizeId ON dbo.ClothingProduct(SizeId);
    CREATE INDEX IX_ClothingProduct_ColorId ON dbo.ClothingProduct(ColorId);
    INSERT dbo.ClothingCategory(Name) VALUES (N'Куртки'),(N'Джемперы'),(N'Жакеты');
    INSERT dbo.ClothingMaterial(Name) VALUES (N'Хлопок 100%'),(N'Хлопок / полиэстер'),(N'Акрил / хлопок'),(N'Полиэстер 100%'),(N'Вискоза / полиэстер'),(N'Шерсть / акрил');
    INSERT dbo.ClothingSize(Name) VALUES (N'104'),(N'110'),(N'116'),(N'122'),(N'128'),(N'134'),(N'140'),(N'146'),(N'152'),(N'158'),(N'164');
    INSERT dbo.ClothingColor(Name) VALUES (N'Синий'),(N'Чёрный'),(N'Бежевый'),(N'Бирюзовый'),(N'Красный'),(N'Жёлтый'),(N'Серый'),(N'Белый');
    INSERT dbo.ClothingProduct(Article,Name,CategoryId,MaterialId,SizeId,ColorId,Price,Stock,Description) VALUES
    (N'JK-001',N'Куртка «Ветер»',1,4,5,1,4290,8,N'Лёгкая демисезонная куртка с капюшоном и застёжкой на молнию. Подкладка из полиэстера.'),
    (N'JK-002',N'Куртка «Капля»',1,4,3,4,3890,6,N'Куртка с водоотталкивающей пропиткой и эластичными манжетами. Два боковых кармана.'),
    (N'JK-003',N'Куртка «Город»',1,2,8,2,5490,4,N'Удлинённая куртка для прогулок. Съёмный капюшон, ветрозащитная планка.'),
    (N'JK-004',N'Куртка «Солнце»',1,4,2,6,3590,7,N'Яркая ветровка без утеплителя. Свободный крой и регулируемый низ.'),
    (N'JK-005',N'Куртка «Облако»',1,4,6,3,6490,3,N'Утеплённая стёганая куртка для прохладной погоды. Наполнитель из полиэфирного волокна.'),
    (N'JK-006',N'Куртка «Пульс»',1,4,10,5,4790,0,N'Спортивная куртка с контрастной молнией. Модель временно отсутствует в наличии.'),
    (N'JK-007',N'Куртка «Деним»',1,1,7,1,3990,5,N'Джинсовая куртка из хлопка. Нагрудные карманы с застёжками на кнопки.'),
    (N'JK-008',N'Куртка «Север»',1,4,11,7,5990,2,N'Куртка с утеплённой подкладкой и высоким воротником. Карманы на молниях.'),
    (N'JM-001',N'Джемпер «Полоска»',2,1,4,1,2190,12,N'Хлопковый джемпер с круглым вырезом и полосатым рисунком. Манжеты и низ в рубчик.'),
    (N'JM-002',N'Джемпер «Мята»',2,3,5,4,2490,9,N'Вязаный джемпер свободного кроя. Мягкая смесовая пряжа с хлопком.'),
    (N'JM-003',N'Джемпер «Тепло»',2,6,8,3,3290,6,N'Джемпер с шерстью и высоким воротником. Однотонная вязка.'),
    (N'JM-004',N'Джемпер «Звезда»',2,1,1,5,1890,10,N'Хлопковый джемпер с декоративным рисунком. Удобный круглый вырез.'),
    (N'JM-005',N'Джемпер «Ритм»',2,3,9,2,2790,8,N'Джемпер с геометрическим узором. Контрастная отделка манжет.'),
    (N'JM-006',N'Джемпер «Снег»',2,6,6,8,3490,4,N'Светлый джемпер крупной вязки. Удлинённая спинка и свободная посадка.'),
    (N'JM-007',N'Джемпер «Лимон»',2,1,3,6,1990,11,N'Лёгкий хлопковый джемпер для повседневной носки. Однотонная модель.'),
    (N'JM-008',N'Джемпер «Туман»',2,3,7,7,2590,0,N'Меланжевый джемпер с круглой горловиной. Ожидается поступление.'),
    (N'JQ-001',N'Жакет «Классика»',3,5,8,2,4590,6,N'Жакет прямого кроя на двух пуговицах. Карманы с клапанами и подкладка.'),
    (N'JQ-002',N'Жакет «Школа»',3,5,6,1,3990,8,N'Школьный жакет с отложным воротником. Подходит для комплекта с брюками или юбкой.'),
    (N'JQ-003',N'Жакет «Песок»',3,2,9,3,4290,5,N'Жакет из смесовой ткани с хлопком. Прямой силуэт и накладные карманы.'),
    (N'JQ-004',N'Жакет «Линия»',3,5,10,7,4890,4,N'Однобортный жакет с застёжкой на одну пуговицу. Подкладка в тон.'),
    (N'JQ-005',N'Жакет «Рубин»',3,5,7,5,4690,3,N'Жакет насыщенного цвета. Классические лацканы и прямой крой.'),
    (N'JQ-006',N'Жакет «Лето»',3,1,5,8,3590,7,N'Лёгкий хлопковый жакет без подкладки. Рукава с декоративной отстрочкой.'),
    (N'JQ-007',N'Жакет «Бирюза»',3,2,4,4,3790,5,N'Повседневный жакет на пуговицах. Хлопковая смесовая ткань.'),
    (N'JQ-008',N'Жакет «Элегант»',3,5,11,2,5190,2,N'Удлинённый жакет с карманами в рамку. Для школьных и торжественных комплектов.');
    COMMIT TRANSACTION;
    SELECT N'База создана' AS Result,
        (SELECT COUNT(*) FROM dbo.ClothingProduct) AS Products,
        (SELECT COUNT(*) FROM dbo.ClothingCategory) AS Categories,
        (SELECT COUNT(*) FROM dbo.ClothingMaterial) AS Materials,
        (SELECT COUNT(*) FROM dbo.ClothingSize) AS Sizes,
        (SELECT COUNT(*) FROM dbo.ClothingColor) AS Colors;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
