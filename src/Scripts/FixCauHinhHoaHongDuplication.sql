-- Script to fix CauHinhHoaHong table duplication issue
-- This script safely handles the duplicate table problem

USE [GymManagement]
GO

PRINT 'Starting CauHinhHoaHong duplication fix...'

-- Step 1: Check if both tables exist
DECLARE @CauHinhHoaHongExists BIT = 0
DECLARE @CauHinhHoaHongsExists BIT = 0

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CauHinhHoaHong')
    SET @CauHinhHoaHongExists = 1

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CauHinhHoaHongs')
    SET @CauHinhHoaHongsExists = 1

PRINT 'Table existence check:'
PRINT '  CauHinhHoaHong (singular): ' + CASE WHEN @CauHinhHoaHongExists = 1 THEN 'EXISTS' ELSE 'NOT EXISTS' END
PRINT '  CauHinhHoaHongs (plural): ' + CASE WHEN @CauHinhHoaHongsExists = 1 THEN 'EXISTS' ELSE 'NOT EXISTS' END

-- Step 2: Handle different scenarios
IF @CauHinhHoaHongExists = 1 AND @CauHinhHoaHongsExists = 1
BEGIN
    PRINT 'SCENARIO: Both tables exist - Need to merge data'
    
    -- Check data in both tables
    DECLARE @SingularCount INT = 0
    DECLARE @PluralCount INT = 0
    
    SELECT @SingularCount = COUNT(*) FROM CauHinhHoaHong
    SELECT @PluralCount = COUNT(*) FROM CauHinhHoaHongs
    
    PRINT '  Data count in CauHinhHoaHong: ' + CAST(@SingularCount AS VARCHAR(10))
    PRINT '  Data count in CauHinhHoaHongs: ' + CAST(@PluralCount AS VARCHAR(10))
    
    IF @SingularCount > 0 AND @PluralCount = 0
    BEGIN
        PRINT '  Migrating data from CauHinhHoaHong to CauHinhHoaHongs...'
        
        -- Migrate data from singular to plural table
        INSERT INTO CauHinhHoaHongs (GoiTapId, PhanTramHoaHong, NgayTao)
        SELECT GoiTapId, PhanTramHoaHong, ISNULL(NgayTao, GETDATE())
        FROM CauHinhHoaHong
        
        PRINT '  Data migration completed.'
    END
    ELSE IF @SingularCount > 0 AND @PluralCount > 0
    BEGIN
        PRINT '  WARNING: Both tables have data. Manual review required!'
        PRINT '  Please review data in both tables before proceeding.'
        
        -- Show data comparison
        PRINT '  Data in CauHinhHoaHong:'
        SELECT * FROM CauHinhHoaHong
        
        PRINT '  Data in CauHinhHoaHongs:'
        SELECT * FROM CauHinhHoaHongs
        
        -- Don't proceed automatically
        RETURN
    END
    
    -- Drop the singular table after successful migration
    IF @SingularCount = 0 OR (@SingularCount > 0 AND @PluralCount = 0)
    BEGIN
        PRINT '  Dropping CauHinhHoaHong table...'
        DROP TABLE CauHinhHoaHong
        PRINT '  CauHinhHoaHong table dropped successfully.'
    END
END
ELSE IF @CauHinhHoaHongExists = 1 AND @CauHinhHoaHongsExists = 0
BEGIN
    PRINT 'SCENARIO: Only CauHinhHoaHong exists - Need to rename to CauHinhHoaHongs'
    
    -- Rename the table to match Entity Framework convention
    EXEC sp_rename 'CauHinhHoaHong', 'CauHinhHoaHongs'
    PRINT '  Table renamed from CauHinhHoaHong to CauHinhHoaHongs'
END
ELSE IF @CauHinhHoaHongExists = 0 AND @CauHinhHoaHongsExists = 1
BEGIN
    PRINT 'SCENARIO: Only CauHinhHoaHongs exists - This is correct, no action needed'
END
ELSE
BEGIN
    PRINT 'SCENARIO: Neither table exists - This might indicate a different issue'
    PRINT 'Please check your database schema and migrations.'
END

-- Step 3: Verify final state
PRINT ''
PRINT 'Final verification:'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CauHinhHoaHongs')
BEGIN
    DECLARE @FinalCount INT
    SELECT @FinalCount = COUNT(*) FROM CauHinhHoaHongs
    PRINT '  ✓ CauHinhHoaHongs table exists with ' + CAST(@FinalCount AS VARCHAR(10)) + ' records'
END
ELSE
BEGIN
    PRINT '  ✗ CauHinhHoaHongs table does not exist'
END

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CauHinhHoaHong')
BEGIN
    PRINT '  ⚠ CauHinhHoaHong table still exists - Manual intervention may be required'
END
ELSE
BEGIN
    PRINT '  ✓ CauHinhHoaHong table does not exist (correct)'
END

-- Step 4: Check foreign key constraints
PRINT ''
PRINT 'Checking foreign key constraints:'

SELECT 
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
WHERE tp.name = 'CauHinhHoaHongs' OR tr.name = 'CauHinhHoaHongs'

PRINT ''
PRINT 'CauHinhHoaHong duplication fix completed!'
PRINT 'Please verify that your application works correctly with the CauHinhHoaHongs table.'

GO
