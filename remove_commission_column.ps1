# PowerShell script to remove TienHoaHong column safely
Write-Host "🔧 Removing TienHoaHong column from BangLuongs table..." -ForegroundColor Cyan

try {
    # Step 1: Build the project to check for compilation errors
    Write-Host "📝 Building project..." -ForegroundColor Yellow
    dotnet build GymManagement.Web/GymManagement.Web.csproj
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed. Please fix compilation errors first." -ForegroundColor Red
        exit 1
    }
    
    # Step 2: Add migration
    Write-Host "📝 Adding migration..." -ForegroundColor Yellow
    dotnet ef migrations add RemoveTienHoaHongColumn --project GymManagement.Web
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to add migration." -ForegroundColor Red
        exit 1
    }
    
    # Step 3: Update database
    Write-Host "🗄️ Updating database..." -ForegroundColor Yellow
    dotnet ef database update --project GymManagement.Web
    
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to update database." -ForegroundColor Red
        exit 1
    }
    
    Write-Host "✅ Successfully removed TienHoaHong column!" -ForegroundColor Green
    Write-Host "📊 Summary of changes:" -ForegroundColor Cyan
    Write-Host "  - Removed TienHoaHong column from database" -ForegroundColor White
    Write-Host "  - Updated BangLuong model" -ForegroundColor White
    Write-Host "  - Simplified TongThanhToan calculation" -ForegroundColor White
    Write-Host "  - Updated views and controllers" -ForegroundColor White
    Write-Host "  - Removed commission-related functionality" -ForegroundColor White
    
} catch {
    Write-Host "❌ Error occurred: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "🎉 Migration completed successfully!" -ForegroundColor Green
