-- Практическая работа 15. Вариант 4: мебель для кухни.
-- Автор проекта: ZaytsevVI. SQL Server 2016+ / SQL Server Express.
-- Запуск: sqlcmd -S ".\SQLEXPRESS" -E -C -b -i Database\01_CreateAndSeed.sql
USE master;
GO
IF DB_ID(N'PR15_KitchenFurniture') IS NULL
    CREATE DATABASE PR15_KitchenFurniture;
GO
USE PR15_KitchenFurniture;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID(N'dbo.FurnitureProduct', N'U') IS NOT NULL
        THROW 50001, N'База уже заполнена. Повторный импорт отменён; существующие данные сохранены.', 1;

    CREATE TABLE dbo.FurnitureCategory
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_FurnitureCategory PRIMARY KEY,
        Name nvarchar(60) NOT NULL CONSTRAINT UQ_FurnitureCategory_Name UNIQUE,
        CONSTRAINT CK_FurnitureCategory_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
    CREATE TABLE dbo.FurnitureMaterial
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_FurnitureMaterial PRIMARY KEY,
        Name nvarchar(60) NOT NULL CONSTRAINT UQ_FurnitureMaterial_Name UNIQUE,
        CONSTRAINT CK_FurnitureMaterial_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0)
    );
    CREATE TABLE dbo.FurnitureProduct
    (
        Id int IDENTITY(1,1) CONSTRAINT PK_FurnitureProduct PRIMARY KEY,
        Article nvarchar(30) NOT NULL CONSTRAINT UQ_FurnitureProduct_Article UNIQUE,
        Name nvarchar(120) NOT NULL,
        CategoryId int NOT NULL CONSTRAINT FK_FurnitureProduct_Category REFERENCES dbo.FurnitureCategory(Id),
        MaterialId int NOT NULL CONSTRAINT FK_FurnitureProduct_Material REFERENCES dbo.FurnitureMaterial(Id),
        Price decimal(12,2) NOT NULL,
        WidthMm int NOT NULL,
        DepthMm int NOT NULL,
        HeightMm int NOT NULL,
        Stock int NOT NULL,
        Description nvarchar(2000) NOT NULL CONSTRAINT DF_FurnitureProduct_Description DEFAULT N'',
        Version rowversion NOT NULL,
        CONSTRAINT CK_FurnitureProduct_Article CHECK (LEN(LTRIM(RTRIM(Article))) > 0),
        CONSTRAINT CK_FurnitureProduct_Name CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_FurnitureProduct_Price CHECK (Price BETWEEN 0 AND 9999999999.99),
        CONSTRAINT CK_FurnitureProduct_Dimensions CHECK (WidthMm BETWEEN 1 AND 10000 AND DepthMm BETWEEN 1 AND 10000 AND HeightMm BETWEEN 1 AND 10000),
        CONSTRAINT CK_FurnitureProduct_Stock CHECK (Stock BETWEEN 0 AND 1000000)
    );
    CREATE INDEX IX_FurnitureProduct_CategoryId ON dbo.FurnitureProduct(CategoryId);
    CREATE INDEX IX_FurnitureProduct_MaterialId ON dbo.FurnitureProduct(MaterialId);

    INSERT dbo.FurnitureCategory(Name) VALUES
        (N'Кухонные гарнитуры'),(N'Напольные шкафы'),(N'Навесные шкафы'),
        (N'Обеденные столы'),(N'Кухонные стулья'),(N'Кухонные уголки');
    INSERT dbo.FurnitureMaterial(Name) VALUES
        (N'ЛДСП'),(N'МДФ'),(N'Массив дуба'),(N'Массив бука'),(N'Металл / МДФ');

    INSERT dbo.FurnitureProduct(Article,Name,CategoryId,MaterialId,Price,WidthMm,DepthMm,HeightMm,Stock,Description) VALUES
    (N'KIT-001',N'Гарнитур «Сканди»',1,2,89900,2400,600,2150,3,N'Прямая кухня с матовыми фасадами. Столешница включена в комплект. Техника и мойка приобретаются отдельно.'),
    (N'KIT-002',N'Гарнитур «Лён»',1,1,54900,2000,600,2100,5,N'Компактный гарнитур для небольшой кухни. Фасады светлого оттенка, распашные дверцы.'),
    (N'KIT-003',N'Гарнитур «Дубовая роща»',1,3,189000,3000,600,2200,1,N'Кухонный гарнитур с фасадами из массива дуба. Корпусные модули поставляются комплектом.'),
    (N'FLR-001',N'Шкаф напольный «Сканди» 600',2,2,12900,600,560,820,12,N'Напольный модуль с полкой. Фасад МДФ, корпус ЛДСП. Столешница в комплект не входит.'),
    (N'FLR-002',N'Тумба под мойку «Лён»',2,1,7900,800,560,820,9,N'Модуль под врезную мойку с двумя дверцами. Задняя стенка открыта для коммуникаций.'),
    (N'FLR-003',N'Шкаф с ящиками «Графит»',2,2,18400,450,560,820,6,N'Три выдвижных ящика с направляющими полного выдвижения. Матовые фасады.'),
    (N'WAL-001',N'Шкаф навесной «Сканди» 600',3,2,8900,600,320,720,15,N'Навесной модуль с одной регулируемой полкой. Монтажная планка приобретается отдельно.'),
    (N'WAL-002',N'Шкаф навесной «Лён» 800',3,1,6900,800,300,720,8,N'Две распашные дверцы, две внутренние полки. Светлый древесный декор.'),
    (N'WAL-003',N'Шкаф-витрина «Дуб»',3,3,23900,500,330,900,0,N'Навесной шкаф с остеклённой дверцей и дубовым фасадом. Ожидается поступление.'),
    (N'TBL-001',N'Стол обеденный «Норд»',4,3,32900,1400,800,750,4,N'Прямоугольная столешница из массива дуба. Рассчитан на четыре–шесть человек.'),
    (N'TBL-002',N'Стол круглый «Круг»',4,5,14500,900,900,750,7,N'Круглая столешница из МДФ и металлическое подстолье. Диаметр 900 мм.'),
    (N'TBL-003',N'Стол раздвижной «Семейный»',4,4,27900,1200,800,760,2,N'Стол из массива бука. Ширина в сложенном виде 1200 мм, после раскладывания 1600 мм.'),
    (N'CHR-001',N'Стул «Норд»',5,3,7900,440,480,850,24,N'Обеденный стул из массива дуба. Высота сиденья 450 мм.'),
    (N'CHR-002',N'Стул «Лайт»',5,5,4900,430,470,820,32,N'Металлический каркас с окрашенным сиденьем из МДФ. Продаётся поштучно.'),
    (N'CHR-003',N'Табурет «Бук»',5,4,2900,350,350,450,18,N'Табурет из массива бука с защитным лаковым покрытием.'),
    (N'COR-001',N'Кухонный уголок «Уют»',6,1,24900,1500,1100,850,3,N'Угловая скамья с мягким сиденьем и отделением для хранения. Обивка из экокожи.'),
    (N'COR-002',N'Кухонный уголок «Тёплый дом»',6,4,38900,1600,1200,880,2,N'Уголок с каркасом из массива бука. Текстильная обивка. Стол не входит в комплект.'),
    (N'FLR-004',N'Пенал для кухни «Графит»',2,2,29900,600,560,2100,4,N'Высокий шкаф с полками для кухонной посуды и запасов продуктов.');
    COMMIT TRANSACTION;
    SELECT N'База создана' AS Result,
        (SELECT COUNT(*) FROM dbo.FurnitureProduct) AS Products,
        (SELECT COUNT(*) FROM dbo.FurnitureCategory) AS Categories,
        (SELECT COUNT(*) FROM dbo.FurnitureMaterial) AS Materials;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
