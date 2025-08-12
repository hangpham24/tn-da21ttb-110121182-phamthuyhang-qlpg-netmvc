# PowerShell script to update ThanhToan table
Write-Host "Updating ThanhToan table to make DangKyId nullable..." -ForegroundColor Yellow

try {
    Write-Host "Dropping foreign key constraint..." -ForegroundColor Cyan
    sqlcmd -S localhost -d QLPG_Remote -E -Q "IF EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_ThanhToans_DangKys_DangKyId') BEGIN ALTER TABLE [ThanhToans] DROP CONSTRAINT [FK_ThanhToans_DangKys_DangKyId] PRINT 'Dropped foreign key constraint' END"

    Write-Host "Making DangKyId nullable..." -ForegroundColor Cyan  
    sqlcmd -S localhost -d QLPG_Remote -E -Q "ALTER TABLE [ThanhToans] ALTER COLUMN [DangKyId] int NULL"

    Write-Host "Re-adding foreign key constraint..." -ForegroundColor Cyan
    sqlcmd -S localhost -d QLPG_Remote -E -Q "ALTER TABLE [ThanhToans] ADD CONSTRAINT [FK_ThanhToans_DangKys_DangKyId] FOREIGN KEY ([DangKyId]) REFERENCES [DangKys] ([DangKyId]) ON DELETE SET NULL"

    Write-Host "Database update completed successfully!" -ForegroundColor Green
}
catch {
    Write-Host "Error occurred: $($_.Exception.Message)" -ForegroundColor Red
} 