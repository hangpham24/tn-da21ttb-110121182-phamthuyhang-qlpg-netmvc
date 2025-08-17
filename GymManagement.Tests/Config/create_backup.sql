-- Create database backup before cleanup
USE HANG_FIX;

DECLARE @BackupPath NVARCHAR(500)
DECLARE @BackupFileName NVARCHAR(500)
DECLARE @BackupFullPath NVARCHAR(500)

-- Create backup filename with timestamp
SET @BackupFileName = 'HANG_FIX_BeforeCleanup_' + FORMAT(GETDATE(), 'yyyyMMdd_HHmmss') + '.bak'
SET @BackupPath = 'C:\Backup\'
SET @BackupFullPath = @BackupPath + @BackupFileName

PRINT '🔄 Creating database backup...'
PRINT 'Backup file: ' + @BackupFullPath

-- Create backup directory if not exists (manual step required)
PRINT 'Note: Please ensure C:\Backup\ directory exists'

-- Create backup
BACKUP DATABASE [HANG_FIX] 
TO DISK = @BackupFullPath
WITH FORMAT, INIT, COMPRESSION, STATS = 10;

PRINT '✅ Database backup completed successfully!'
PRINT 'Backup location: ' + @BackupFullPath
