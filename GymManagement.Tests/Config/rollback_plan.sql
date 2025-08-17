-- =====================================================
-- ROLLBACK PLAN: Restore LichLops and KhuyenMaiUsages Tables
-- =====================================================
-- Author: Gym Management System
-- Date: 2025-01-13
-- Purpose: Emergency rollback if cleanup causes issues

-- =====================================================
-- EMERGENCY ROLLBACK PROCEDURE
-- =====================================================

PRINT '🚨 EMERGENCY ROLLBACK INITIATED'
PRINT 'This will restore the database to pre-cleanup state'

-- Step 1: Restore from backup
PRINT 'Step 1: Restoring database from backup...'
PRINT 'Manual step required:'
PRINT '1. Stop the application'
PRINT '2. Run: RESTORE DATABASE [GymManagementDb] FROM DISK = ''C:\Backup\GymManagementDb_BeforeCleanup_YYYYMMDD_HHMMSS.bak'' WITH REPLACE'
PRINT '3. Restart the application'

-- Alternative: Recreate tables manually if backup restore is not possible
PRINT ''
PRINT '=== ALTERNATIVE: Manual Table Recreation ==='

-- Recreate LichLops table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops')
BEGIN
    PRINT 'Recreating LichLops table...'
    
    CREATE TABLE [dbo].[LichLops](
        [LichLopId] [int] IDENTITY(1,1) NOT NULL,
        [LopHocId] [int] NOT NULL,
        [Ngay] [date] NOT NULL,
        [GioBatDau] [time](7) NOT NULL,
        [GioKetThuc] [time](7) NOT NULL,
        [TrangThai] [nvarchar](20) NOT NULL DEFAULT 'SCHEDULED',
        [SoLuongDaDat] [int] NOT NULL DEFAULT 0,
        CONSTRAINT [PK_LichLops] PRIMARY KEY CLUSTERED ([LichLopId] ASC)
    )
    
    -- Add foreign key constraint
    ALTER TABLE [dbo].[LichLops] 
    ADD CONSTRAINT [FK_LichLops_LopHocs_LopHocId] 
    FOREIGN KEY([LopHocId]) REFERENCES [dbo].[LopHocs] ([LopHocId])
    ON DELETE CASCADE
    
    -- Add check constraints
    ALTER TABLE [dbo].[LichLops] 
    ADD CONSTRAINT [CK_LichLop_Status] 
    CHECK ([TrangThai] IN ('SCHEDULED', 'CANCELLED', 'FINISHED'))
    
    ALTER TABLE [dbo].[LichLops] 
    ADD CONSTRAINT [CK_LichLop_TimeRange] 
    CHECK ([GioKetThuc] > [GioBatDau])
    
    -- Add index
    CREATE NONCLUSTERED INDEX [IX_LichLop_LopHocId_Ngay] 
    ON [dbo].[LichLops] ([LopHocId], [Ngay])
    
    PRINT '✅ LichLops table recreated'
END

-- Recreate KhuyenMaiUsages table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages')
BEGIN
    PRINT 'Recreating KhuyenMaiUsages table...'
    
    CREATE TABLE [dbo].[KhuyenMaiUsages](
        [KhuyenMaiUsageId] [int] IDENTITY(1,1) NOT NULL,
        [KhuyenMaiId] [int] NOT NULL,
        [NguoiDungId] [int] NOT NULL,
        [ThanhToanId] [int] NULL,
        [DangKyId] [int] NULL,
        [SoTienGoc] [decimal](18, 2) NOT NULL,
        [SoTienGiam] [decimal](18, 2) NOT NULL,
        [SoTienCuoi] [decimal](18, 2) NOT NULL,
        [NgaySuDung] [datetime2](7) NOT NULL,
        [GhiChu] [nvarchar](500) NULL,
        CONSTRAINT [PK_KhuyenMaiUsages] PRIMARY KEY CLUSTERED ([KhuyenMaiUsageId] ASC)
    )
    
    -- Add foreign key constraints
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_KhuyenMais_KhuyenMaiId] 
    FOREIGN KEY([KhuyenMaiId]) REFERENCES [dbo].[KhuyenMais] ([KhuyenMaiId])
    ON DELETE CASCADE
    
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_NguoiDungs_NguoiDungId] 
    FOREIGN KEY([NguoiDungId]) REFERENCES [dbo].[NguoiDungs] ([NguoiDungId])
    ON DELETE CASCADE
    
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_ThanhToans_ThanhToanId] 
    FOREIGN KEY([ThanhToanId]) REFERENCES [dbo].[ThanhToans] ([ThanhToanId])
    
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_DangKys_DangKyId] 
    FOREIGN KEY([DangKyId]) REFERENCES [dbo].[DangKys] ([DangKyId])
    
    -- Add indexes
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_KhuyenMaiId] 
    ON [dbo].[KhuyenMaiUsages] ([KhuyenMaiId])
    
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_NguoiDungId] 
    ON [dbo].[KhuyenMaiUsages] ([NguoiDungId])
    
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_NgaySuDung] 
    ON [dbo].[KhuyenMaiUsages] ([NgaySuDung])
    
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_ThanhToanId] 
    ON [dbo].[KhuyenMaiUsages] ([ThanhToanId])
    
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_DangKyId] 
    ON [dbo].[KhuyenMaiUsages] ([DangKyId])
    
    PRINT '✅ KhuyenMaiUsages table recreated'
END

-- Recreate foreign key constraints from other tables
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops')
BEGIN
    -- Add FK from Bookings to LichLops
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Bookings_LichLops_LichLopId')
    BEGIN
        ALTER TABLE [dbo].[Bookings] 
        ADD CONSTRAINT [FK_Bookings_LichLops_LichLopId] 
        FOREIGN KEY([LichLopId]) REFERENCES [dbo].[LichLops] ([LichLopId])
        PRINT '✅ Added FK_Bookings_LichLops_LichLopId'
    END
    
    -- Add FK from DiemDanhs to LichLops
    IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_DiemDanhs_LichLops_LichLopId')
    BEGIN
        ALTER TABLE [dbo].[DiemDanhs] 
        ADD CONSTRAINT [FK_DiemDanhs_LichLops_LichLopId] 
        FOREIGN KEY([LichLopId]) REFERENCES [dbo].[LichLops] ([LichLopId])
        PRINT '✅ Added FK_DiemDanhs_LichLops_LichLopId'
    END
END

-- Verification
PRINT ''
PRINT '🔍 Rollback Verification:'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'LichLops')
    PRINT '✅ LichLops table exists'
ELSE
    PRINT '❌ LichLops table missing'

IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages')
    PRINT '✅ KhuyenMaiUsages table exists'
ELSE
    PRINT '❌ KhuyenMaiUsages table missing'

-- Check foreign key constraints
SELECT 
    'Rollback Verification' AS CheckType,
    fk.name AS ForeignKeyName,
    tp.name AS ParentTable,
    tr.name AS ReferencedTable
FROM sys.foreign_keys fk
INNER JOIN sys.tables tp ON fk.parent_object_id = tp.object_id
INNER JOIN sys.tables tr ON fk.referenced_object_id = tr.object_id
WHERE tr.name IN ('LichLops', 'KhuyenMaiUsages')
ORDER BY tr.name, fk.name

PRINT ''
PRINT '✅ Rollback completed. Please restore code files from version control.'
PRINT ''
PRINT '📋 Manual steps required:'
PRINT '1. Restore deleted model files from git'
PRINT '2. Restore DbContext changes'
PRINT '3. Restore service implementations'
PRINT '4. Restore controller actions'
PRINT '5. Run: dotnet ef database update'
PRINT '6. Rebuild and test application'
