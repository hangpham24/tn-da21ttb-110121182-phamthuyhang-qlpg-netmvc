-- Fix Migration History for HANG_FIX Database
-- This script manually inserts migration records to sync with existing database structure

USE [HANG_FIX];

-- Check if __EFMigrationsHistory table exists
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') AND type in (N'U'))
BEGIN
    PRINT 'Creating __EFMigrationsHistory table...'
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END

-- Clear existing migration history (if any)
DELETE FROM [__EFMigrationsHistory];

-- Insert migration records in chronological order
INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES 
('20250721151538_CustomAuthSystem', '8.0.0'),
('20250723135839_AddMoTaAndThoiLuongToLopHoc', '8.0.0'),
('20250724132844_FixDbContextConfiguration', '8.0.0'),
('20250812051928_AddMissingDiemDanhColumns', '8.0.0'),
('20250813142951_RemoveLichLopAndKhuyenMaiUsage', '8.0.0');

-- Verify the migration history
SELECT 
    [MigrationId], 
    [ProductVersion],
    'Applied' as Status
FROM [__EFMigrationsHistory] 
ORDER BY [MigrationId];

PRINT 'Migration history has been fixed successfully!'
PRINT 'Total migrations applied: ' + CAST(@@ROWCOUNT AS VARCHAR(10))
