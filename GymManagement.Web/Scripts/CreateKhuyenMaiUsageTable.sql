-- Script to create KhuyenMaiUsages table for promotion usage tracking
USE [QLPG_Remote]
GO

PRINT 'Creating KhuyenMaiUsages table...'

-- Check if table already exists
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'KhuyenMaiUsages')
BEGIN
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

    PRINT '✓ KhuyenMaiUsages table created successfully'
END
ELSE
BEGIN
    PRINT '⚠ KhuyenMaiUsages table already exists'
END

-- Add foreign key constraints
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_KhuyenMaiUsages_KhuyenMais_KhuyenMaiId')
BEGIN
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_KhuyenMais_KhuyenMaiId] 
    FOREIGN KEY([KhuyenMaiId]) REFERENCES [dbo].[KhuyenMais] ([KhuyenMaiId])
    ON DELETE CASCADE
    
    PRINT '✓ Foreign key constraint to KhuyenMais added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_KhuyenMaiUsages_NguoiDungs_NguoiDungId')
BEGIN
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_NguoiDungs_NguoiDungId] 
    FOREIGN KEY([NguoiDungId]) REFERENCES [dbo].[NguoiDungs] ([NguoiDungId])
    ON DELETE CASCADE
    
    PRINT '✓ Foreign key constraint to NguoiDungs added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_KhuyenMaiUsages_ThanhToans_ThanhToanId')
BEGIN
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_ThanhToans_ThanhToanId] 
    FOREIGN KEY([ThanhToanId]) REFERENCES [dbo].[ThanhToans] ([ThanhToanId])
    
    PRINT '✓ Foreign key constraint to ThanhToans added'
END

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_KhuyenMaiUsages_DangKys_DangKyId')
BEGIN
    ALTER TABLE [dbo].[KhuyenMaiUsages] 
    ADD CONSTRAINT [FK_KhuyenMaiUsages_DangKys_DangKyId] 
    FOREIGN KEY([DangKyId]) REFERENCES [dbo].[DangKys] ([DangKyId])
    
    PRINT '✓ Foreign key constraint to DangKys added'
END

-- Add indexes for better performance
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KhuyenMaiUsages_KhuyenMaiId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_KhuyenMaiId] 
    ON [dbo].[KhuyenMaiUsages] ([KhuyenMaiId])
    
    PRINT '✓ Index on KhuyenMaiId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KhuyenMaiUsages_NguoiDungId')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_NguoiDungId] 
    ON [dbo].[KhuyenMaiUsages] ([NguoiDungId])
    
    PRINT '✓ Index on NguoiDungId created'
END

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_KhuyenMaiUsages_NgaySuDung')
BEGIN
    CREATE NONCLUSTERED INDEX [IX_KhuyenMaiUsages_NgaySuDung] 
    ON [dbo].[KhuyenMaiUsages] ([NgaySuDung])
    
    PRINT '✓ Index on NgaySuDung created'
END

-- Add NgayTao column to KhuyenMais table if it doesn't exist
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'KhuyenMais' AND COLUMN_NAME = 'NgayTao')
BEGIN
    ALTER TABLE [dbo].[KhuyenMais] 
    ADD [NgayTao] [datetime2](7) NOT NULL DEFAULT GETDATE()
    
    PRINT '✓ NgayTao column added to KhuyenMais table'
END
ELSE
BEGIN
    PRINT '⚠ NgayTao column already exists in KhuyenMais table'
END

PRINT ''
PRINT 'KhuyenMaiUsages table setup completed successfully!'
PRINT 'You can now use the promotion tracking system.'

-- Verify the setup
PRINT ''
PRINT 'Verification:'
SELECT 
    TABLE_NAME,
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS 
WHERE TABLE_NAME IN ('KhuyenMais', 'KhuyenMaiUsages')
ORDER BY TABLE_NAME, ORDINAL_POSITION

GO
