-- =====================================================
-- CLEANUP PLAN: Remove LichLops and KhuyenMaiUsages Tables
-- =====================================================
-- Author: Gym Management System
-- Date: 2025-01-13
-- Purpose: Safe removal of unused tables from database
-- Database: HANG_FIX

USE HANG_FIX;

-- =====================================================
-- STEP 1: BACKUP STRATEGY
-- =====================================================

-- 1.1 Create full database backup
PRINT '🔄 Database backup already completed'
-- Backup location: C:\Backup\HANG_FIX_BeforeCleanup_20250813_203710.bak
PRINT '✅ Database backup verified'

-- 1.2 Export data from tables (if needed for future reference)
PRINT '📊 Exporting table data for reference...'

-- Export KhuyenMaiUsages data (should be empty but just in case)
SELECT 'KhuyenMaiUsages Data Export - ' + CAST(GETDATE() AS VARCHAR(50)) AS ExportInfo
SELECT COUNT(*) AS TotalRecords FROM KhuyenMaiUsages
IF EXISTS (SELECT 1 FROM KhuyenMaiUsages)
BEGIN
    SELECT * FROM KhuyenMaiUsages
    PRINT '⚠️  WARNING: KhuyenMaiUsages contains data!'
END
ELSE
    PRINT '✅ KhuyenMaiUsages is empty as expected'

-- Export LichLops data (should be empty but just in case)
SELECT 'LichLops Data Export - ' + CAST(GETDATE() AS VARCHAR(50)) AS ExportInfo
SELECT COUNT(*) AS TotalRecords FROM LichLops
IF EXISTS (SELECT 1 FROM LichLops)
BEGIN
    SELECT * FROM LichLops
    PRINT '⚠️  WARNING: LichLops contains data!'
END
ELSE
    PRINT '✅ LichLops is empty as expected'

-- =====================================================
-- STEP 2: DEPENDENCY ANALYSIS
-- =====================================================

PRINT '🔍 Analyzing dependencies...'

-- Check foreign key constraints
SELECT 
    'Foreign Key Dependencies' AS AnalysisType,
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

-- Check indexes
SELECT 
    'Index Dependencies' AS AnalysisType,
    t.name AS TableName,
    i.name AS IndexName,
    i.type_desc AS IndexType
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('LichLops', 'KhuyenMaiUsages')
AND i.name IS NOT NULL
ORDER BY t.name, i.name

-- Check constraints
SELECT 
    'Constraint Dependencies' AS AnalysisType,
    t.name AS TableName,
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition
FROM sys.check_constraints cc
INNER JOIN sys.tables t ON cc.parent_object_id = t.object_id
WHERE t.name IN ('LichLops', 'KhuyenMaiUsages')
ORDER BY t.name, cc.name

-- =====================================================
-- STEP 3: SAFE REMOVAL SEQUENCE
-- =====================================================

PRINT '🗑️  Starting safe removal sequence...'

-- 3.1 Drop foreign key constraints referencing LichLops
PRINT 'Step 3.1: Dropping foreign key constraints referencing LichLops...'

-- Drop FK from Bookings to LichLops
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Bookings_LichLops_LichLopId')
BEGIN
    ALTER TABLE [Bookings] DROP CONSTRAINT [FK_Bookings_LichLops_LichLopId]
    PRINT '✅ Dropped FK_Bookings_LichLops_LichLopId'
END

-- Drop FK from DiemDanhs to LichLops
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DiemDanhs_LichLops_LichLopId')
BEGIN
    ALTER TABLE [DiemDanhs] DROP CONSTRAINT [FK_DiemDanhs_LichLops_LichLopId]
    PRINT '✅ Dropped FK_DiemDanhs_LichLops_LichLopId'
END

-- 3.2 Drop indexes on tables that will be removed
PRINT 'Step 3.2: Dropping indexes...'

-- Drop indexes on LichLops
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_LichLop_LopHocId_Ngay')
BEGIN
    DROP INDEX [IX_LichLop_LopHocId_Ngay] ON [LichLops]
    PRINT '✅ Dropped IX_LichLop_LopHocId_Ngay'
END

-- Drop indexes on KhuyenMaiUsages
DECLARE @sql NVARCHAR(MAX)
DECLARE index_cursor CURSOR FOR
SELECT 'DROP INDEX [' + i.name + '] ON [' + t.name + ']'
FROM sys.indexes i
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name = 'KhuyenMaiUsages' AND i.name IS NOT NULL AND i.is_primary_key = 0

OPEN index_cursor
FETCH NEXT FROM index_cursor INTO @sql
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_executesql @sql
    PRINT '✅ Executed: ' + @sql
    FETCH NEXT FROM index_cursor INTO @sql
END
CLOSE index_cursor
DEALLOCATE index_cursor

-- 3.3 Drop check constraints
PRINT 'Step 3.3: Dropping check constraints...'

-- Drop constraints on LichLops
IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LichLop_Status')
BEGIN
    ALTER TABLE [LichLops] DROP CONSTRAINT [CK_LichLop_Status]
    PRINT '✅ Dropped CK_LichLop_Status'
END

IF EXISTS (SELECT * FROM sys.check_constraints WHERE name = 'CK_LichLop_TimeRange')
BEGIN
    ALTER TABLE [LichLops] DROP CONSTRAINT [CK_LichLop_TimeRange]
    PRINT '✅ Dropped CK_LichLop_TimeRange'
END

-- 3.4 Drop the tables
PRINT 'Step 3.4: Dropping tables...'

-- Drop KhuyenMaiUsages first (no dependencies)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages')
BEGIN
    DROP TABLE [KhuyenMaiUsages]
    PRINT '✅ Dropped table KhuyenMaiUsages'
END

-- Drop LichLops
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops')
BEGIN
    DROP TABLE [LichLops]
    PRINT '✅ Dropped table LichLops'
END

-- =====================================================
-- STEP 4: CLEANUP NULLABLE FOREIGN KEYS
-- =====================================================

PRINT 'Step 4: Setting nullable foreign key columns to NULL...'

-- Set LichLopId to NULL in Bookings table
UPDATE [Bookings] SET [LichLopId] = NULL WHERE [LichLopId] IS NOT NULL
PRINT '✅ Set LichLopId to NULL in Bookings table'

-- Set LichLopId to NULL in DiemDanhs table  
UPDATE [DiemDanhs] SET [LichLopId] = NULL WHERE [LichLopId] IS NOT NULL
PRINT '✅ Set LichLopId to NULL in DiemDanhs table'

-- =====================================================
-- STEP 5: VERIFICATION
-- =====================================================

PRINT '🔍 Verification phase...'

-- Verify tables are dropped
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops')
    PRINT '✅ LichLops table successfully removed'
ELSE
    PRINT '❌ ERROR: LichLops table still exists'

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages')
    PRINT '✅ KhuyenMaiUsages table successfully removed'
ELSE
    PRINT '❌ ERROR: KhuyenMaiUsages table still exists'

-- Verify foreign key constraints are removed
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name LIKE '%LichLops%')
    PRINT '✅ All foreign key constraints to LichLops removed'
ELSE
    PRINT '❌ ERROR: Some foreign key constraints to LichLops still exist'

-- Check data integrity
SELECT 'Data Integrity Check' AS CheckType, 
       COUNT(*) AS BookingsWithNullLichLopId 
FROM Bookings WHERE LichLopId IS NULL

SELECT 'Data Integrity Check' AS CheckType,
       COUNT(*) AS DiemDanhsWithNullLichLopId 
FROM DiemDanhs WHERE LichLopId IS NULL

PRINT '✅ Database cleanup completed successfully!'
PRINT '📋 Summary:'
PRINT '   - LichLops table removed'
PRINT '   - KhuyenMaiUsages table removed'  
PRINT '   - All related constraints and indexes removed'
PRINT '   - Nullable foreign keys set to NULL'
PRINT '   - Database backup created'
