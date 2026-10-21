USE master;
GO
IF DB_ID(N'Task09School') IS NULL CREATE DATABASE Task09School;
GO
USE Task09School;
GO
IF OBJECT_ID(N'dbo.Service') IS NOT NULL THROW 50000, N'База уже содержит таблицы. Импорт остановлен без изменения данных.', 1;
GO

/****** Object:  Table [dbo].[AttachedProduct]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[AttachedProduct](
	[MainProductID] [int] NOT NULL,
	[AttachedProductID] [int] NOT NULL,
 CONSTRAINT [PK_AttachedProduct] PRIMARY KEY CLUSTERED
(
	[MainProductID] ASC,
	[AttachedProductID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Client]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Client](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[FirstName] [nvarchar](50) NOT NULL,
	[LastName] [nvarchar](50) NOT NULL,
	[Patronymic] [nvarchar](50) NULL,
	[Birthday] [date] NULL,
	[RegistrationDate] [datetime] NOT NULL,
	[Email] [nvarchar](255) NULL,
	[Phone] [nvarchar](20) NOT NULL,
	[GenderCode] [nchar](1) NOT NULL,
	[PhotoPath] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Client] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ClientService]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ClientService](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ClientID] [int] NOT NULL,
	[ServiceID] [int] NOT NULL,
	[StartTime] [datetime] NOT NULL,
	[Comment] [nvarchar](max) NULL,
 CONSTRAINT [PK_ClientService] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DocumentByService]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DocumentByService](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ClientServiceID] [int] NOT NULL,
	[DocumentPath] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_DocumentByService] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Gender]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Gender](
	[Code] [nchar](1) NOT NULL,
	[Name] [nvarchar](10) NULL,
 CONSTRAINT [PK_Gender] PRIMARY KEY CLUSTERED
(
	[Code] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Manufacturer]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Manufacturer](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[StartDate] [date] NULL,
 CONSTRAINT [PK_Manufacturer] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Product]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Product](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Cost] [money] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[MainImagePath] [nvarchar](1000) NULL,
	[IsActive] [bit] NOT NULL,
	[ManufacturerID] [int] NULL,
 CONSTRAINT [PK_Product] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductPhoto]    Script Date: 12/2/2019 10:23:09 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductPhoto](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ProductID] [int] NOT NULL,
	[PhotoPath] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_ProductPhoto] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ProductSale]    Script Date: 12/2/2019 10:23:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ProductSale](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[SaleDate] [datetime] NOT NULL,
	[ProductID] [int] NOT NULL,
	[Quantity] [int] NOT NULL,
	[ClientServiceID] [int] NULL,
 CONSTRAINT [PK_ProductSale] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Service]    Script Date: 12/2/2019 10:23:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Service](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](100) NOT NULL,
	[Cost] [money] NOT NULL,
	[DurationInSeconds] [int] NOT NULL,
	[Description] [nvarchar](max) NULL,
	[Discount] [float] NULL,
	[MainImagePath] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Service] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[ServicePhoto]    Script Date: 12/2/2019 10:23:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[ServicePhoto](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[ServiceID] [int] NOT NULL,
	[PhotoPath] [nvarchar](1000) NOT NULL,
 CONSTRAINT [PK_ServicePhoto] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Tag]    Script Date: 12/2/2019 10:23:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Tag](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Title] [nvarchar](30) NOT NULL,
	[Color] [nchar](6) NOT NULL,
 CONSTRAINT [PK_Tag] PRIMARY KEY CLUSTERED
(
	[ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[TagOfClient]    Script Date: 12/2/2019 10:23:10 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[TagOfClient](
	[ClientID] [int] NOT NULL,
	[TagID] [int] NOT NULL,
 CONSTRAINT [PK_TagOfClient] PRIMARY KEY CLUSTERED
(
	[ClientID] ASC,
	[TagID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AttachedProduct]  WITH CHECK ADD  CONSTRAINT [FK_AttachedProduct_Product] FOREIGN KEY([MainProductID])
REFERENCES [dbo].[Product] ([ID])
GO
ALTER TABLE [dbo].[AttachedProduct] CHECK CONSTRAINT [FK_AttachedProduct_Product]
GO
ALTER TABLE [dbo].[AttachedProduct]  WITH CHECK ADD  CONSTRAINT [FK_AttachedProduct_Product1] FOREIGN KEY([AttachedProductID])
REFERENCES [dbo].[Product] ([ID])
GO
ALTER TABLE [dbo].[AttachedProduct] CHECK CONSTRAINT [FK_AttachedProduct_Product1]
GO
ALTER TABLE [dbo].[Client]  WITH CHECK ADD  CONSTRAINT [FK_Client_Gender] FOREIGN KEY([GenderCode])
REFERENCES [dbo].[Gender] ([Code])
GO
ALTER TABLE [dbo].[Client] CHECK CONSTRAINT [FK_Client_Gender]
GO
ALTER TABLE [dbo].[ClientService]  WITH CHECK ADD  CONSTRAINT [FK_ClientService_Client] FOREIGN KEY([ClientID])
REFERENCES [dbo].[Client] ([ID])
GO
ALTER TABLE [dbo].[ClientService] CHECK CONSTRAINT [FK_ClientService_Client]
GO
ALTER TABLE [dbo].[ClientService]  WITH CHECK ADD  CONSTRAINT [FK_ClientService_Service] FOREIGN KEY([ServiceID])
REFERENCES [dbo].[Service] ([ID])
GO
ALTER TABLE [dbo].[ClientService] CHECK CONSTRAINT [FK_ClientService_Service]
GO
ALTER TABLE [dbo].[DocumentByService]  WITH CHECK ADD  CONSTRAINT [FK_DocumentByService_ClientService] FOREIGN KEY([ClientServiceID])
REFERENCES [dbo].[ClientService] ([ID])
GO
ALTER TABLE [dbo].[DocumentByService] CHECK CONSTRAINT [FK_DocumentByService_ClientService]
GO
ALTER TABLE [dbo].[Product]  WITH CHECK ADD  CONSTRAINT [FK_Product_Manufacturer] FOREIGN KEY([ManufacturerID])
REFERENCES [dbo].[Manufacturer] ([ID])
GO
ALTER TABLE [dbo].[Product] CHECK CONSTRAINT [FK_Product_Manufacturer]
GO
ALTER TABLE [dbo].[ProductPhoto]  WITH CHECK ADD  CONSTRAINT [FK_ProductPhoto_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([ID])
GO
ALTER TABLE [dbo].[ProductPhoto] CHECK CONSTRAINT [FK_ProductPhoto_Product]
GO
ALTER TABLE [dbo].[ProductSale]  WITH CHECK ADD  CONSTRAINT [FK_ProductSale_ClientService] FOREIGN KEY([ClientServiceID])
REFERENCES [dbo].[ClientService] ([ID])
GO
ALTER TABLE [dbo].[ProductSale] CHECK CONSTRAINT [FK_ProductSale_ClientService]
GO
ALTER TABLE [dbo].[ProductSale]  WITH CHECK ADD  CONSTRAINT [FK_ProductSale_Product] FOREIGN KEY([ProductID])
REFERENCES [dbo].[Product] ([ID])
GO
ALTER TABLE [dbo].[ProductSale] CHECK CONSTRAINT [FK_ProductSale_Product]
GO
ALTER TABLE [dbo].[ServicePhoto]  WITH CHECK ADD  CONSTRAINT [FK_ServicePhoto_Service] FOREIGN KEY([ServiceID])
REFERENCES [dbo].[Service] ([ID])
GO
ALTER TABLE [dbo].[ServicePhoto] CHECK CONSTRAINT [FK_ServicePhoto_Service]
GO
ALTER TABLE [dbo].[TagOfClient]  WITH CHECK ADD  CONSTRAINT [FK_TagOfClient_Client] FOREIGN KEY([ClientID])
REFERENCES [dbo].[Client] ([ID])
GO
ALTER TABLE [dbo].[TagOfClient] CHECK CONSTRAINT [FK_TagOfClient_Client]
GO
ALTER TABLE [dbo].[TagOfClient]  WITH CHECK ADD  CONSTRAINT [FK_TagOfClient_Tag] FOREIGN KEY([TagID])
REFERENCES [dbo].[Tag] ([ID])
GO
ALTER TABLE [dbo].[TagOfClient] CHECK CONSTRAINT [FK_TagOfClient_Tag]
GO
GO
SET XACT_ABORT ON;
BEGIN TRANSACTION;
INSERT Gender(Code,Name) VALUES (N'м',N'Мужской'),(N'ж',N'Женский');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем китайского языка',1950,7200,0.0,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок немецкого языка с преподавателем-носителем языка',1340,6600,0.0,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб китайского языка для студентов',1990,6000,0.0,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный онлайн-урок японского языка по Skype',1000,4800,0.2,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб испанского языка для студентов',1050,2400,0.1,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором испанского языка',1450,3000,0.15,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе итальянского языка для взрослых',1290,2400,0.0,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс французского языка для компаний по Skype',1180,2400,0.1,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок французского языка с преподавателем-носителем языка',1410,2400,0.2,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе французского языка для школьников',1970,6000,0.0,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем английского языка',910,6600,0.0,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб французского языка для взрослых',1770,5400,0.1,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб немецкого языка для взрослых',1560,6000,0.1,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе японского языка для школьников',1300,4800,0.05,N'Assets/Услуги школы/Японский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок испанского языка с русскоязычным преподавателем',1790,3600,0.15,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс итальянского языка для компаний по Skype',1230,4200,0.1,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе японского языка для студентов',1860,3000,0.0,N'Assets/Услуги школы/Японский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем английского языка для компаний',1670,6600,0.1,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб итальянского языка для студентов',1760,1800,0.0,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе испанского языка для взрослых',1730,1800,0.05,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе испанского языка для студентов',1310,6000,0.2,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс испанского языка для компаний по Skype',1510,7200,0.0,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе английского языка для школьников',900,6600,0.0,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем испанского языка',1090,3000,0.05,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем итальянского языка для компаний',1190,2400,0.2,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе китайского языка для студентов',2000,2400,0.2,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок испанского языка с преподавателем-носителем языка',1200,5400,0.1,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс португальского языка для компаний по Skype',1610,4200,0.05,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену ACT',1440,3000,0.0,N'Assets/Услуги школы/Подготовка к экзамену ACT.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе китайского языка для взрослых',1730,4200,0.25,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс английского языка для компаний по Skype',1160,3000,0.0,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену TOEFL',1070,6000,0.05,N'Assets/Услуги школы/Подготовка к экзамену TOEFL.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем итальянского языка',1370,7200,0.05,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе английского языка для взрослых',1010,3000,0.25,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб английского языка для взрослых',1250,4200,0.05,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем французского языка для компаний',1630,4200,0.15,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе испанского языка для школьников',1910,7200,0.0,N'Assets/Услуги школы/Испанский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену GRE',1300,4800,0.2,N'Assets/Услуги школы/Подготовка к экзамену GRE.jpeg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе английского языка для студентов',960,4800,0.25,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором японского языка',1260,2400,0.1,N'Assets/Услуги школы/Японский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный онлайн-урок немецкого языка по Skype',970,5400,0.0,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс японского языка с русскоязычным преподавателем для компаний',1500,4800,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб китайского языка для взрослых',1800,4200,0.25,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок китайского языка с русскоязычным преподавателем',1860,7200,0.1,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену GMAT',1150,4200,0.05,N'Assets/Услуги школы/Подготовка к экзамену GMAT.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену IELTS',1730,7200,0.0,N'Assets/Услуги школы/Подготовка к экзамену IELTS.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену SAT',1560,2400,0.0,N'Assets/Услуги школы/Подготовка к экзамену SAT.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе китайского языка для школьников',1180,3000,0.15,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб английского языка для студентов',980,4800,0.0,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб португальского языка для студентов',1170,1800,0.15,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб китайского языка для детей',1120,6000,0.1,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе немецкого языка для школьников',1570,1800,0.15,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный онлайн-урок испанского языка по Skype',1780,4200,0.05,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе немецкого языка для взрослых',930,3600,0.05,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок китайского языка с преподавателем-носителем языка',1480,5400,0.2,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором португальского языка',1970,3000,0.1,N'Assets/Услуги школы/Португальский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс китайского языка с русскоязычным преподавателем для компаний',1470,6600,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором немецкого языка',1420,1800,0.0,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб английского языка для детей',1280,2400,0.05,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем немецкого языка',1120,7200,0.0,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный онлайн-урок китайского языка по Skype',1410,4200,0.0,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок английского языка с преподавателем-носителем языка',1300,7200,0.0,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем немецкого языка для компаний',1180,7200,0.2,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс немецкого языка для компаний по Skype',1840,4200,0.1,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе итальянского языка для школьников',1410,6000,0.15,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок итальянского языка с русскоязычным преподавателем',1330,1800,0.2,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный онлайн-курс китайского языка для компаний по Skype',1760,7200,0.15,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором французского языка',1870,6000,0.2,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок немецкого языка с русскоязычным преподавателем',990,1800,0.05,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный онлайн-урок английского языка по Skype',1880,6000,0.0,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок французского языка с русскоязычным преподавателем',1390,4200,0.0,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс португальского языка с русскоязычным преподавателем для компаний',2010,3600,0.25,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный онлайн-урок французского языка по Skype',2010,7200,0.0,N'Assets/Услуги школы/online lessons.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс английского языка с русскоязычным преподавателем для компаний',1210,4800,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе французского языка для взрослых',960,7200,0.15,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Подготовка к экзамену IELTS Speaking Club',1430,5400,0.2,N'Assets/Услуги школы/Подготовка к экзамену IELTS.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб итальянского языка для детей',1480,4200,0.0,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе итальянского языка для студентов',1020,6600,0.0,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок итальянского языка с преподавателем-носителем языка',1690,4800,0.0,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб немецкого языка для детей',1670,7200,0.05,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб итальянского языка для взрослых',1760,6600,0.0,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб португальского языка для взрослых',950,1800,0.05,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб португальского языка для детей',1710,5400,0.15,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб французского языка для детей',1600,5400,0.1,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе японского языка для взрослых',1420,4800,0.05,N'Assets/Услуги школы/Японский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс французского языка с русскоязычным преподавателем для компаний',1940,2400,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором итальянского языка',1620,4200,0.2,N'Assets/Услуги школы/Итальянский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Индивидуальный урок английского языка с русскоязычным преподавателем',1910,1800,0.1,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем португальского языка для компаний',1580,1800,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс испанского языка с русскоязычным преподавателем для компаний',1000,5400,0.25,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Киноклуб немецкого языка для студентов',1140,3000,0.15,N'Assets/Услуги школы/киноклуб.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем французского языка',2040,3000,0.0,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе немецкого языка для студентов',1240,4200,0.0,N'Assets/Услуги школы/Немецкий язык.png');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором китайского языка',940,6600,0.15,N'Assets/Услуги школы/Китайский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с репетитором-носителем японского языка',1100,3000,0.25,N'Assets/Услуги школы/Японский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Урок в группе французского языка для студентов',1910,1800,0.0,N'Assets/Услуги школы/Французский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс итальянского языка с русскоязычным преподавателем для компаний',900,6600,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Занятие с русскоязычным репетитором английского языка',1950,5400,0.15,N'Assets/Услуги школы/Английский язык.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем испанского языка для компаний',1660,5400,0.0,N'Assets/Услуги школы/for company.jpg');
INSERT Service(Title,Cost,DurationInSeconds,Discount,MainImagePath) VALUES(N'Интенсивный курс с преподавателем-носителем японского языка для компаний',1380,4200,0.2,N'Assets/Услуги школы/for company.jpg');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Васильев',N'Оскар',N'Богданович',N'м',N'7(585)801-94-29',N'1971-01-30T00:00:00',N'miturria@verizon.net',N'2017-05-28T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Лазарев',N'Алексей',N'Богданович',N'м',N'7(0055)737-37-48',N'1977-03-10T00:00:00',N'claesjac@me.com',N'2017-01-02T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Казаков',N'Дмитрий',N'Русланович',N'м',N'7(51)682-19-40',N'1978-12-15T00:00:00',N'ozawa@verizon.net',N'2016-05-21T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Попова',N'Харита',N'Якуновна',N'ж',N'7(335)386-81-06',N'1997-12-16T00:00:00',N'firstpr@verizon.net',N'2016-07-05T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Турова',N'Георгина',N'Семёновна',N'ж',N'7(555)321-42-99',N'1974-05-28T00:00:00',N'yruan@optonline.net',N'2018-02-22T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Андреев',N'Станислав',N'Максович',N'м',N'7(02)993-91-28',N'1975-10-10T00:00:00',N'budinger@mac.com',N'2017-12-26T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Орлова',N'Влада',N'Мартыновна',N'ж',N'7(2506)433-38-35',N'1990-06-26T00:00:00',N'rnelson@yahoo.ca',N'2016-03-21T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Костина',N'Любава',N'Авксентьевна',N'ж',N'7(6419)959-21-87',N'1972-07-13T00:00:00',N'gordonjcp@hotmail.com',N'2016-02-26T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Александров',N'Станислав',N'Эдуардович',N'м',N'7(18)164-05-12',N'1981-07-04T00:00:00',N'bigmauler@aol.com',N'2018-11-08T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Корнилова',N'Анэля',N'Михайловна',N'ж',N'7(20)980-01-60',N'1973-04-02T00:00:00',N'jonathan@aol.com',N'2016-05-22T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Морозов',N'Наум',N'Валерьянович',N'м',N'7(636)050-96-13',N'1985-07-04T00:00:00',N'salesgeek@mac.com',N'2016-05-02T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Баранова',N'Эльмира',N'Дмитриевна',N'ж',N'7(9240)643-15-50',N'1977-01-15T00:00:00',N'jgmyers@comcast.net',N'2016-07-08T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Степанова',N'Амелия',N'Робертовна',N'ж',N'7(1217)441-28-42',N'1970-06-06T00:00:00',N'rasca@hotmail.com',N'2017-09-27T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Горбачёв',N'Давид',N'Тимурович',N'м',N'7(53)602-85-41',N'1983-05-22T00:00:00',N'hedwig@att.net',N'2018-12-17T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Волков',N'Людвиг',N'Витальевич',N'м',N'7(8459)592-05-58',N'1977-12-27T00:00:00',N'jrkorson@msn.com',N'2016-04-27T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Комиссарова',N'Амалия',N'Робертовна',N'ж',N'7(22)647-46-32',N'1971-08-18T00:00:00',N'jorgb@msn.com',N'2017-08-04T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Большаков',N'Вадим',N'Данилович',N'м',N'7(386)641-13-37',N'1970-05-15T00:00:00',N'uncle@gmail.com',N'2018-08-04T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Абрамов',N'Станислав',N'Филатович',N'м',N'7(6545)478-87-79',N'1989-05-18T00:00:00',N'solomon@att.net',N'2016-12-08T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Журавлёв',N'Леонтий',N'Яковлевич',N'м',N'7(4403)308-56-96',N'2000-03-02T00:00:00',N'cmdrgravy@me.com',N'2018-01-15T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Селиверстов',N'Глеб',N'Максимович',N'м',N'7(20)554-28-68',N'1999-06-20T00:00:00',N'jigsaw@sbcglobal.net',N'2016-01-07T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Киселёв',N'Устин',N'Яковлевич',N'м',N'7(83)334-52-76',N'1985-01-08T00:00:00',N'dalamb@verizon.net',N'2018-06-21T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Зуев',N'Матвей',N'Иванович',N'м',N'7(5383)893-04-66',N'1981-03-28T00:00:00',N'brickbat@verizon.net',N'2018-12-18T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Ершов',N'Глеб',N'Федорович',N'м',N'7(2608)298-40-82',N'1970-06-14T00:00:00',N'sjava@aol.com',N'2016-09-14T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Бобылёв',N'Георгий',N'Витальевич',N'м',N'7(88)685-13-51',N'1983-12-19T00:00:00',N'csilvers@mac.com',N'2018-04-06T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Ефремов',N'Витольд',N'Авксентьевич',N'м',N'7(93)922-14-03',N'1975-12-02T00:00:00',N'kwilliams@yahoo.ca',N'2018-04-09T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Андреева',N'Патрисия',N'Валерьевна',N'ж',N'7(9648)953-81-26',N'1993-11-18T00:00:00',N'jigsaw@aol.com',N'2016-07-17T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Ефимова',N'Магда',N'Платоновна',N'ж',N'7(9261)386-15-92',N'1995-08-16T00:00:00',N'rbarreira@me.com',N'2017-08-01T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Голубев',N'Иосиф',N'Тимофеевич',N'м',N'7(78)972-73-11',N'1982-05-06T00:00:00',N'smcnabb@att.net',N'2018-08-18T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Евсеев',N'Макар',N'Васильевич',N'м',N'7(2141)077-85-70',N'1977-09-13T00:00:00',N'parsimony@sbcglobal.net',N'2018-12-05T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Бобров',N'Агафон',N'Лаврентьевич',N'м',N'7(2159)507-39-57',N'1995-07-29T00:00:00',N'petersen@comcast.net',N'2017-05-09T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Игнатов',N'Захар',N'Павлович',N'м',N'7(578)574-73-36',N'1998-10-07T00:00:00',N'dieman@icloud.com',N'2017-11-10T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Гущина',N'Янита',N'Федоровна',N'ж',N'7(4544)716-68-96',N'1999-03-02T00:00:00',N'lishoy@att.net',N'2018-02-01T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Калашников',N'Артур',N'Юрьевич',N'м',N'7(147)947-47-21',N'1972-12-13T00:00:00',N'oevans@aol.com',N'2017-08-20T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Дмитриева',N'Элина',N'Даниловна',N'ж',N'7(787)140-48-84',N'1988-12-10T00:00:00',N'vmalik@live.com',N'2017-02-11T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Федотова',N'Сандра',N'Владленовна',N'ж',N'7(126)195-25-86',N'1985-03-29T00:00:00',N'penna@verizon.net',N'2016-11-08T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Кузьмина',N'Дэнна',N'Витальевна',N'ж',N'7(9940)977-45-73',N'1993-08-24T00:00:00',N'nichoj@mac.com',N'2016-03-27T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Колобова',N'Злата',N'Романовна',N'ж',N'7(50)884-07-35',N'1994-08-25T00:00:00',N'sinkou@aol.com',N'2016-12-03T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Некрасов',N'Варлам',N'Михайлович',N'м',N'7(019)258-06-35',N'2000-11-12T00:00:00',N'dogdude@verizon.net',N'2017-12-03T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Колобов',N'Орест',N'Юлианович',N'м',N'7(1680)508-58-26',N'2001-07-14T00:00:00',N'parkes@verizon.net',N'2017-01-01T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Блохин',N'Пантелеймон',N'Феликсович',N'м',N'7(9524)556-48-98',N'1978-03-06T00:00:00',N'balchen@comcast.net',N'2018-02-14T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Калинин',N'Петр',N'Иванович',N'м',N'7(90)316-07-17',N'1993-09-08T00:00:00',N'aschmitz@hotmail.com',N'2016-05-26T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Беляева',N'Сабрина',N'Федосеевна',N'ж',N'7(6580)534-32-58',N'1972-07-26T00:00:00',N'agapow@gmail.com',N'2017-06-14T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Агафонов',N'Юстиниан',N'Олегович',N'м',N'7(303)810-28-78',N'1997-02-02T00:00:00',N'staffelb@sbcglobal.net',N'2016-06-08T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Мамонтова',N'Марфа',N'Мироновна',N'ж',N'7(38)095-64-18',N'1984-10-19T00:00:00',N'rfoley@verizon.net',N'2018-02-27T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Блинов',N'Евгений',N'Мэлсович',N'м',N'7(0852)321-82-64',N'1994-01-05T00:00:00',N'moxfulder@outlook.com',N'2017-05-07T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Воронова',N'Изабелла',N'Вячеславовна',N'ж',N'7(17)433-44-98',N'1999-09-24T00:00:00',N'kildjean@sbcglobal.net',N'2017-12-21T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Медведев',N'Святослав',N'Юлианович',N'м',N'7(3520)435-21-20',N'1972-10-04T00:00:00',N'hllam@comcast.net',N'2018-10-13T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Куликова',N'Эвелина',N'Вячеславовна',N'ж',N'7(0236)682-42-78',N'1997-11-14T00:00:00',N'ilikered@hotmail.com',N'2018-02-01T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Суворова',N'Божена',N'Анатольевна',N'ж',N'7(347)895-86-57',N'1981-03-09T00:00:00',N'attwood@aol.com',N'2016-01-28T00:00:00');
INSERT Client(LastName,FirstName,Patronymic,GenderCode,Phone,Birthday,Email,RegistrationDate) VALUES(N'Егорова',N'Амалия',N'Дамировна',N'ж',N'7(7486)408-12-26',N'1999-09-28T00:00:00',N'drezet@yahoo.com',N'2016-06-30T00:00:00');
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Суворова' ORDER BY ID), ID,N'2019-11-16T11:20:00' FROM Service WHERE Title=N'Урок в группе французского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Колобова' ORDER BY ID), ID,N'2019-01-11T18:10:00' FROM Service WHERE Title=N'Индивидуальный онлайн-урок немецкого языка по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Колобова' ORDER BY ID), ID,N'2019-12-01T14:40:00' FROM Service WHERE Title=N'Киноклуб итальянского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Колобова' ORDER BY ID), ID,N'2019-02-11T13:30:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс португальского языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Селиверстов' ORDER BY ID), ID,N'2019-09-10T18:20:00' FROM Service WHERE Title=N'Урок в группе китайского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Куликова' ORDER BY ID), ID,N'2019-02-12T19:40:00' FROM Service WHERE Title=N'Урок в группе китайского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобылёв' ORDER BY ID), ID,N'2019-04-04T09:10:00' FROM Service WHERE Title=N'Занятие с репетитором-носителем французского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Киселёв' ORDER BY ID), ID,N'2019-05-15T09:20:00' FROM Service WHERE Title=N'Урок в группе испанского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Комиссарова' ORDER BY ID), ID,N'2019-01-16T14:20:00' FROM Service WHERE Title=N'Урок в группе немецкого языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобров' ORDER BY ID), ID,N'2019-01-16T09:00:00' FROM Service WHERE Title=N'Урок в группе английского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Казаков' ORDER BY ID), ID,N'2019-03-15T10:50:00' FROM Service WHERE Title=N'Урок в группе немецкого языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Андреева' ORDER BY ID), ID,N'2019-01-31T12:00:00' FROM Service WHERE Title=N'Индивидуальный онлайн-урок немецкого языка по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Андреев' ORDER BY ID), ID,N'2019-11-19T15:50:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс немецкого языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Калашников' ORDER BY ID), ID,N'2019-01-10T08:30:00' FROM Service WHERE Title=N'Подготовка к экзамену TOEFL';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Федотова' ORDER BY ID), ID,N'2019-04-29T15:00:00' FROM Service WHERE Title=N'Урок в группе немецкого языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Агафонов' ORDER BY ID), ID,N'2019-10-12T08:50:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Куликова' ORDER BY ID), ID,N'2019-02-13T18:00:00' FROM Service WHERE Title=N'Урок в группе китайского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Калашников' ORDER BY ID), ID,N'2019-02-01T18:00:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс немецкого языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобров' ORDER BY ID), ID,N'2019-11-12T18:50:00' FROM Service WHERE Title=N'Интенсивный курс с преподавателем-носителем немецкого языка для компаний';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Большаков' ORDER BY ID), ID,N'2019-01-11T12:50:00' FROM Service WHERE Title=N'Киноклуб португальского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Александров' ORDER BY ID), ID,N'2019-03-23T19:50:00' FROM Service WHERE Title=N'Киноклуб португальского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Агафонов' ORDER BY ID), ID,N'2019-06-23T08:00:00' FROM Service WHERE Title=N'Киноклуб французского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Некрасов' ORDER BY ID), ID,N'2019-05-25T17:10:00' FROM Service WHERE Title=N'Интенсивный курс с преподавателем-носителем английского языка для компаний';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Большаков' ORDER BY ID), ID,N'2019-02-18T18:20:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс китайского языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Федотова' ORDER BY ID), ID,N'2019-07-17T08:20:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Федотова' ORDER BY ID), ID,N'2019-08-06T16:50:00' FROM Service WHERE Title=N'Занятие с репетитором-носителем французского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Васильев' ORDER BY ID), ID,N'2019-12-14T08:40:00' FROM Service WHERE Title=N'Киноклуб итальянского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Зуев' ORDER BY ID), ID,N'2019-06-28T14:20:00' FROM Service WHERE Title=N'Подготовка к экзамену IELTS Speaking Club';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Куликова' ORDER BY ID), ID,N'2019-06-06T19:20:00' FROM Service WHERE Title=N'Интенсивный курс итальянского языка с русскоязычным преподавателем для компаний';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобылёв' ORDER BY ID), ID,N'2019-06-07T17:20:00' FROM Service WHERE Title=N'Киноклуб португальского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Воронова' ORDER BY ID), ID,N'2019-04-30T18:10:00' FROM Service WHERE Title=N'Урок в группе английского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Андреев' ORDER BY ID), ID,N'2019-08-30T11:40:00' FROM Service WHERE Title=N'Киноклуб португальского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ефремов' ORDER BY ID), ID,N'2019-05-08T14:10:00' FROM Service WHERE Title=N'Урок в группе испанского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Беляева' ORDER BY ID), ID,N'2019-10-27T16:20:00' FROM Service WHERE Title=N'Индивидуальный урок итальянского языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобров' ORDER BY ID), ID,N'2019-01-05T08:40:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Зуев' ORDER BY ID), ID,N'2019-03-04T17:20:00' FROM Service WHERE Title=N'Индивидуальный урок испанского языка с преподавателем-носителем языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Егорова' ORDER BY ID), ID,N'2019-09-29T13:40:00' FROM Service WHERE Title=N'Индивидуальный урок немецкого языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ефимова' ORDER BY ID), ID,N'2019-01-22T10:50:00' FROM Service WHERE Title=N'Киноклуб китайского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобров' ORDER BY ID), ID,N'2019-12-19T19:30:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс немецкого языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Гущина' ORDER BY ID), ID,N'2019-11-15T17:50:00' FROM Service WHERE Title=N'Подготовка к экзамену ACT';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Баранова' ORDER BY ID), ID,N'2019-07-04T17:10:00' FROM Service WHERE Title=N'Индивидуальный урок французского языка с преподавателем-носителем языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Киселёв' ORDER BY ID), ID,N'2019-08-06T11:50:00' FROM Service WHERE Title=N'Индивидуальный онлайн-урок немецкого языка по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Федотова' ORDER BY ID), ID,N'2019-11-09T13:00:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Бобылёв' ORDER BY ID), ID,N'2019-02-16T18:50:00' FROM Service WHERE Title=N'Индивидуальный урок английского языка с преподавателем-носителем языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ершов' ORDER BY ID), ID,N'2019-03-03T17:20:00' FROM Service WHERE Title=N'Индивидуальный урок немецкого языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Степанова' ORDER BY ID), ID,N'2019-05-22T16:10:00' FROM Service WHERE Title=N'Урок в группе английского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Кузьмина' ORDER BY ID), ID,N'2019-07-31T18:10:00' FROM Service WHERE Title=N'Урок в группе французского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Комиссарова' ORDER BY ID), ID,N'2019-08-29T19:00:00' FROM Service WHERE Title=N'Индивидуальный урок испанского языка с преподавателем-носителем языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Киселёв' ORDER BY ID), ID,N'2019-10-02T10:10:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Костина' ORDER BY ID), ID,N'2019-03-16T11:30:00' FROM Service WHERE Title=N'Индивидуальный онлайн-урок немецкого языка по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Агафонов' ORDER BY ID), ID,N'2019-08-27T11:40:00' FROM Service WHERE Title=N'Подготовка к экзамену ACT';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Абрамов' ORDER BY ID), ID,N'2019-06-13T11:30:00' FROM Service WHERE Title=N'Индивидуальный урок итальянского языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Попова' ORDER BY ID), ID,N'2019-08-18T16:00:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Горбачёв' ORDER BY ID), ID,N'2019-12-26T16:30:00' FROM Service WHERE Title=N'Урок в группе английского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Морозов' ORDER BY ID), ID,N'2019-11-27T18:50:00' FROM Service WHERE Title=N'Урок в группе итальянского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Медведев' ORDER BY ID), ID,N'2019-09-06T19:20:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Воронова' ORDER BY ID), ID,N'2019-09-24T13:40:00' FROM Service WHERE Title=N'Занятие с русскоязычным репетитором английского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Гущина' ORDER BY ID), ID,N'2019-03-01T11:50:00' FROM Service WHERE Title=N'Интенсивный курс с преподавателем-носителем португальского языка для компаний';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Турова' ORDER BY ID), ID,N'2019-09-28T10:00:00' FROM Service WHERE Title=N'Урок в группе итальянского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Волков' ORDER BY ID), ID,N'2019-03-20T10:40:00' FROM Service WHERE Title=N'Занятие с репетитором-носителем французского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Корнилова' ORDER BY ID), ID,N'2019-11-27T11:30:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Казаков' ORDER BY ID), ID,N'2019-01-23T18:30:00' FROM Service WHERE Title=N'Занятие с репетитором-носителем немецкого языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ефимова' ORDER BY ID), ID,N'2019-10-21T10:20:00' FROM Service WHERE Title=N'Индивидуальный урок испанского языка с преподавателем-носителем языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Голубев' ORDER BY ID), ID,N'2019-08-11T12:50:00' FROM Service WHERE Title=N'Занятие с репетитором-носителем французского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Орлова' ORDER BY ID), ID,N'2019-01-03T14:40:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Калашников' ORDER BY ID), ID,N'2019-12-31T08:30:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс немецкого языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Калинин' ORDER BY ID), ID,N'2019-08-29T10:10:00' FROM Service WHERE Title=N'Урок в группе испанского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Степанова' ORDER BY ID), ID,N'2019-01-03T18:50:00' FROM Service WHERE Title=N'Киноклуб итальянского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Горбачёв' ORDER BY ID), ID,N'2019-04-09T11:10:00' FROM Service WHERE Title=N'Урок в группе китайского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ефимова' ORDER BY ID), ID,N'2019-12-31T19:10:00' FROM Service WHERE Title=N'Киноклуб французского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Лазарев' ORDER BY ID), ID,N'2019-10-17T13:30:00' FROM Service WHERE Title=N'Киноклуб итальянского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Воронова' ORDER BY ID), ID,N'2019-05-17T13:30:00' FROM Service WHERE Title=N'Индивидуальный урок немецкого языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Морозов' ORDER BY ID), ID,N'2019-11-23T18:40:00' FROM Service WHERE Title=N'Урок в группе японского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Корнилова' ORDER BY ID), ID,N'2019-07-10T15:30:00' FROM Service WHERE Title=N'Киноклуб португальского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Колобова' ORDER BY ID), ID,N'2019-06-09T13:40:00' FROM Service WHERE Title=N'Занятие с русскоязычным репетитором японского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ершов' ORDER BY ID), ID,N'2019-01-09T10:20:00' FROM Service WHERE Title=N'Индивидуальный урок немецкого языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Лазарев' ORDER BY ID), ID,N'2019-08-15T18:30:00' FROM Service WHERE Title=N'Занятие с репетитором-носителем немецкого языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Евсеев' ORDER BY ID), ID,N'2019-07-15T14:10:00' FROM Service WHERE Title=N'Киноклуб португальского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Киселёв' ORDER BY ID), ID,N'2019-12-31T18:00:00' FROM Service WHERE Title=N'Урок в группе японского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Гущина' ORDER BY ID), ID,N'2019-12-22T09:20:00' FROM Service WHERE Title=N'Урок в группе итальянского языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Горбачёв' ORDER BY ID), ID,N'2019-12-28T16:00:00' FROM Service WHERE Title=N'Киноклуб китайского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Калашников' ORDER BY ID), ID,N'2019-06-26T10:10:00' FROM Service WHERE Title=N'Индивидуальный урок немецкого языка с русскоязычным преподавателем';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Корнилова' ORDER BY ID), ID,N'2019-04-29T12:40:00' FROM Service WHERE Title=N'Урок в группе французского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Куликова' ORDER BY ID), ID,N'2019-04-13T09:00:00' FROM Service WHERE Title=N'Урок в группе французского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ершов' ORDER BY ID), ID,N'2019-01-05T16:00:00' FROM Service WHERE Title=N'Урок в группе китайского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Турова' ORDER BY ID), ID,N'2019-03-25T18:20:00' FROM Service WHERE Title=N'Занятие с русскоязычным репетитором японского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Евсеев' ORDER BY ID), ID,N'2019-11-09T18:50:00' FROM Service WHERE Title=N'Интенсивный курс с преподавателем-носителем английского языка для компаний';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Игнатов' ORDER BY ID), ID,N'2019-04-07T10:40:00' FROM Service WHERE Title=N'Индивидуальный онлайн-урок немецкого языка по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Суворова' ORDER BY ID), ID,N'2019-11-11T18:50:00' FROM Service WHERE Title=N'Интенсивный онлайн-курс китайского языка для компаний по Skype';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Дмитриева' ORDER BY ID), ID,N'2019-01-06T15:00:00' FROM Service WHERE Title=N'Киноклуб немецкого языка для взрослых';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Попова' ORDER BY ID), ID,N'2019-08-20T19:10:00' FROM Service WHERE Title=N'Урок в группе китайского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Блинов' ORDER BY ID), ID,N'2019-04-16T13:20:00' FROM Service WHERE Title=N'Урок в группе китайского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Ефимова' ORDER BY ID), ID,N'2019-12-22T15:10:00' FROM Service WHERE Title=N'Урок в группе немецкого языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Блохин' ORDER BY ID), ID,N'2019-01-22T16:50:00' FROM Service WHERE Title=N'Киноклуб португальского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Костина' ORDER BY ID), ID,N'2019-09-04T08:10:00' FROM Service WHERE Title=N'Интенсивный курс с преподавателем-носителем английского языка для компаний';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Калинин' ORDER BY ID), ID,N'2019-01-01T14:50:00' FROM Service WHERE Title=N'Киноклуб итальянского языка для детей';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Федотова' ORDER BY ID), ID,N'2019-01-29T16:30:00' FROM Service WHERE Title=N'Урок в группе японского языка для школьников';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Блохин' ORDER BY ID), ID,N'2019-07-06T11:30:00' FROM Service WHERE Title=N'Киноклуб английского языка для студентов';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Голубев' ORDER BY ID), ID,N'2019-12-05T10:20:00' FROM Service WHERE Title=N'Занятие с русскоязычным репетитором английского языка';
INSERT ClientService(ClientID,ServiceID,StartTime) SELECT (SELECT TOP(1) ID FROM Client WHERE LastName=N'Селиверстов' ORDER BY ID), ID,N'2019-04-05T13:20:00' FROM Service WHERE Title=N'Урок в группе китайского языка для школьников';
ALTER TABLE Service ADD CONSTRAINT CK_Service_Cost CHECK(Cost>=0), CONSTRAINT CK_Service_Duration CHECK(DurationInSeconds BETWEEN 0 AND 14400), CONSTRAINT CK_Service_Discount CHECK(Discount IS NULL OR Discount BETWEEN 0 AND 1);
CREATE UNIQUE INDEX UX_Service_Title ON Service(Title);
CREATE INDEX IX_ClientService_StartTime ON ClientService(StartTime);
COMMIT;
GO
SELECT (SELECT COUNT(*) FROM Service) AS Services,(SELECT COUNT(*) FROM Client) AS Clients,(SELECT COUNT(*) FROM ClientService) AS Bookings;
