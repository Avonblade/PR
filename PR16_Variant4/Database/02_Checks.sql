USE PR16_AlisaClothes;
GO
SET NOCOUNT ON;
SELECT DB_NAME() AS DatabaseName,
       (SELECT COUNT(*) FROM dbo.ClothingProduct) AS Products,
       (SELECT COUNT(*) FROM dbo.ClothingCategory) AS Categories,
       (SELECT COUNT(*) FROM dbo.ClothingMaterial) AS Materials,
       (SELECT COUNT(*) FROM dbo.ClothingSize) AS Sizes,
       (SELECT COUNT(*) FROM dbo.ClothingColor) AS Colors,
       (SELECT SUM(Stock) FROM dbo.ClothingProduct) AS StockTotal;

SELECT p.Id, p.Article, p.Name, c.Name AS Category, m.Name AS Material,
       s.Name AS Size, col.Name AS Color, p.Price, p.Stock
FROM dbo.ClothingProduct p
JOIN dbo.ClothingCategory c ON c.Id = p.CategoryId
JOIN dbo.ClothingMaterial m ON m.Id = p.MaterialId
JOIN dbo.ClothingSize s ON s.Id = p.SizeId
JOIN dbo.ClothingColor col ON col.Id = p.ColorId
ORDER BY p.Id;

SELECT p.Id AS OrphanProductId
FROM dbo.ClothingProduct p
LEFT JOIN dbo.ClothingCategory c ON c.Id = p.CategoryId
LEFT JOIN dbo.ClothingMaterial m ON m.Id = p.MaterialId
LEFT JOIN dbo.ClothingSize s ON s.Id = p.SizeId
LEFT JOIN dbo.ClothingColor col ON col.Id = p.ColorId
WHERE c.Id IS NULL OR m.Id IS NULL OR s.Id IS NULL OR col.Id IS NULL;

SELECT Article, COUNT(*) AS DuplicateCount
FROM dbo.ClothingProduct GROUP BY Article HAVING COUNT(*) > 1;

DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS;

SELECT fk.name AS ForeignKeyName, OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.ClothingProduct');
GO
