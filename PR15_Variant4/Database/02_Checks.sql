-- Проверка базы после установки; запросы не изменяют данные.
USE PR15_KitchenFurniture;
GO
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName,
       (SELECT COUNT(*) FROM dbo.FurnitureProduct) AS Products,
       (SELECT COUNT(*) FROM dbo.FurnitureCategory) AS Categories,
       (SELECT COUNT(*) FROM dbo.FurnitureMaterial) AS Materials,
       (SELECT SUM(Stock) FROM dbo.FurnitureProduct) AS StockTotal;
-- Для исходного наполнения ожидается: 18 товаров / 6 категорий / 5 материалов / 155 единиц.

SELECT p.Id, p.Article, p.Name, c.Name AS Category, m.Name AS Material,
       p.Price, p.WidthMm, p.DepthMm, p.HeightMm, p.Stock, p.Description
FROM dbo.FurnitureProduct p
JOIN dbo.FurnitureCategory c ON p.CategoryId = c.Id
JOIN dbo.FurnitureMaterial m ON p.MaterialId = m.Id
ORDER BY p.Id;

-- Ожидаются пустые результаты трёх следующих проверок.
SELECT p.Id AS OrphanProductId
FROM dbo.FurnitureProduct p
LEFT JOIN dbo.FurnitureCategory c ON p.CategoryId = c.Id
LEFT JOIN dbo.FurnitureMaterial m ON p.MaterialId = m.Id
WHERE c.Id IS NULL OR m.Id IS NULL;

SELECT Article, COUNT(*) AS DuplicateCount
FROM dbo.FurnitureProduct GROUP BY Article HAVING COUNT(*) > 1;

DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

SELECT t.name AS TableName, fk.name AS ForeignKeyName,
       OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
JOIN sys.tables t ON t.object_id = fk.parent_object_id
WHERE t.name = 'FurnitureProduct';
GO
