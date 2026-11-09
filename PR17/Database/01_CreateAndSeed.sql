USE master;
GO
IF DB_ID(N'PR17_ZebraRestaurant') IS NULL
    EXEC(N'CREATE DATABASE PR17_ZebraRestaurant');
GO
USE PR17_ZebraRestaurant;
GO
SET XACT_ABORT ON;
SET NOCOUNT ON;

BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.Dishes', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.Categories', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.Cuisines', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.ServingSizes', N'U') IS NOT NULL
   OR OBJECT_ID(N'dbo.Users', N'U') IS NOT NULL
    THROW 50001, N'Таблицы ПР17 уже существуют. Данные сохранены.', 1;

CREATE TABLE dbo.Categories (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(40) NOT NULL UNIQUE
);
CREATE TABLE dbo.Cuisines (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(40) NOT NULL UNIQUE
);
CREATE TABLE dbo.ServingSizes (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(40) NOT NULL UNIQUE
);
CREATE TABLE dbo.Users (
    Id int IDENTITY PRIMARY KEY,
    Login nvarchar(50) NOT NULL UNIQUE,
    DisplayName nvarchar(80) NOT NULL,
    PasswordSalt binary(16) NOT NULL,
    PasswordHash binary(32) NOT NULL,
    IsActive bit NOT NULL DEFAULT 1
);
CREATE TABLE dbo.Dishes (
    Id int IDENTITY PRIMARY KEY,
    Name nvarchar(100) NOT NULL CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
    CategoryId int NOT NULL REFERENCES dbo.Categories(Id),
    CuisineId int NOT NULL REFERENCES dbo.Cuisines(Id),
    ServingSizeId int NOT NULL REFERENCES dbo.ServingSizes(Id),
    Price decimal(9,2) NOT NULL CHECK (Price > 0),
    Description nvarchar(500) NOT NULL DEFAULT N'',
    Version rowversion NOT NULL
);

INSERT dbo.Categories (Name) VALUES (N'Закуски'), (N'Супы'), (N'Горячее'), (N'Десерты'), (N'Напитки');
INSERT dbo.Cuisines (Name) VALUES (N'Авторская'), (N'Русская'), (N'Итальянская'), (N'Азиатская');
INSERT dbo.ServingSizes (Name) VALUES (N'Малая'), (N'Стандартная'), (N'Большая');
INSERT dbo.Users (Login, DisplayName, PasswordSalt, PasswordHash) VALUES
    (N'editor', N'Редактор меню', 0x32ad998937d2ff8a8133ffd759255643, 0xd5c52aa9ba15c4131c40c422e172049ac963b33ceb5a0c478bc93dd3564c736f);

INSERT dbo.Dishes (Name, CategoryId, CuisineId, ServingSizeId, Price, Description) VALUES
    (N'Брускетта с томатами', 1, 3, 1, 320, N'Хрустящий хлеб, томаты, базилик и оливковое масло.'),
    (N'Салат с печёной свёклой', 1, 1, 2, 390, N'Печёная свёкла с мягким сыром, рукколой и орехами.'),
    (N'Спринг-роллы с овощами', 1, 4, 2, 410, N'Овощи в рисовой бумаге с кунжутной заправкой.'),
    (N'Борщ со сметаной', 2, 2, 2, 360, N'Свекольный суп с капустой, говядиной и сметаной.'),
    (N'Тыквенный крем-суп', 2, 1, 2, 350, N'Запечённая тыква, сливки и поджаренные семечки.'),
    (N'Рамен с курицей', 2, 4, 3, 540, N'Пшеничная лапша, куриный бульон, яйцо и зелёный лук.'),
    (N'Паста с грибами', 3, 3, 2, 590, N'Паста в сливочном соусе с грибами и пармезаном.'),
    (N'Котлета по-киевски', 3, 2, 2, 620, N'Куриная котлета с зелёным маслом и картофельным пюре.'),
    (N'Лосось терияки', 3, 4, 3, 870, N'Лосось в соусе терияки с рисом и брокколи.'),
    (N'Панна-котта с малиной', 4, 3, 1, 320, N'Сливочный десерт с малиновым соусом.'),
    (N'Мармеладная зебра', 4, 1, 2, 390, N'Слоёный ванильный десерт с ягодным мармеладом.'),
    (N'Облепиховый морс', 5, 2, 2, 240, N'Охлаждённый напиток из облепихи.'),
    (N'Лимонад с маракуйей', 5, 1, 3, 290, N'Маракуйя, лайм и газированная вода.');

COMMIT;
GO
