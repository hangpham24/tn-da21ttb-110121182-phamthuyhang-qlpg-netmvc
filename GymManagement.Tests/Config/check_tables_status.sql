-- Check current status of LichLops and KhuyenMaiUsages tables
USE HANG_FIX;

PRINT '🔍 Checking current status of tables...'

-- Check if tables exist
SELECT 
    'Table Existence Check' AS CheckType,
    CASE 
        WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops') 
        THEN 'EXISTS' 
        ELSE 'NOT EXISTS' 
    END AS LichLopsStatus,
    CASE 
        WHEN EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages') 
        THEN 'EXISTS' 
        ELSE 'NOT EXISTS' 
    END AS KhuyenMaiUsagesStatus

-- Check record counts if tables exist
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops')
BEGIN
    SELECT 'LichLops' as TableName, COUNT(*) as RecordCount FROM LichLops
END
ELSE
    SELECT 'LichLops' as TableName, 'TABLE NOT EXISTS' as RecordCount

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages')
BEGIN
    SELECT 'KhuyenMaiUsages' as TableName, COUNT(*) as RecordCount FROM KhuyenMaiUsages
END
ELSE
    SELECT 'KhuyenMaiUsages' as TableName, 'TABLE NOT EXISTS' as RecordCount

-- Check foreign key dependencies
SELECT 
    'Foreign Key Dependencies' AS CheckType,
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    cp.name AS ParentColumn,
    tr.name AS ReferencedTable,
    cr.name AS ReferencedColumn
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns cp ON fkc.parent_column_id = cp.column_id AND fkc.parent_object_id = cp.object_id
INNER JOIN sys.columns cr ON fkc.referenced_column_id = cr.column_id AND fkc.referenced_object_id = cr.object_id
WHERE tp.name IN ('LichLops', 'KhuyenMaiUsages') OR tr.name IN ('LichLops', 'KhuyenMaiUsages')
ORDER BY tp.name, fk.name

PRINT '✅ Status check completed'
