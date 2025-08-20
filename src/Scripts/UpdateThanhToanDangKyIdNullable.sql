-- Update ThanhToan table to make DangKyId nullable
-- Run this manually in SQL Server Management Studio

-- First, drop foreign key constraint temporarily
IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ThanhToans_DangKys_DangKyId')
BEGIN
    ALTER TABLE [ThanhToans] DROP CONSTRAINT [FK_ThanhToans_DangKys_DangKyId]
END

-- Make DangKyId nullable
ALTER TABLE [ThanhToans] ALTER COLUMN [DangKyId] int NULL

-- Re-add foreign key constraint with nullable support
ALTER TABLE [ThanhToans] 
ADD CONSTRAINT [FK_ThanhToans_DangKys_DangKyId] 
FOREIGN KEY ([DangKyId]) REFERENCES [DangKys] ([DangKyId])
ON DELETE SET NULL

PRINT 'ThanhToan.DangKyId is now nullable and foreign key constraint updated' 