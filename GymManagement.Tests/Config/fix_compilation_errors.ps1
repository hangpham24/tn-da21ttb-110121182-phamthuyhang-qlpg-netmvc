# PowerShell script to fix compilation errors after LichLop removal
Write-Host "🔧 Fixing compilation errors after LichLop removal..." -ForegroundColor Yellow

$projectPath = "tn-da21ttb-110121182-phamthuyhang-qlpg-netmvc\GymManagement.Web"

# Function to replace text in file
function Replace-InFile {
    param(
        [string]$FilePath,
        [string]$OldText,
        [string]$NewText
    )
    
    if (Test-Path $FilePath) {
        $content = Get-Content $FilePath -Raw
        if ($content -match [regex]::Escape($OldText)) {
            $content = $content -replace [regex]::Escape($OldText), $NewText
            Set-Content $FilePath $content -NoNewline
            Write-Host "✅ Fixed: $FilePath" -ForegroundColor Green
        }
    }
}

# Function to remove lines containing specific text
function Remove-LinesContaining {
    param(
        [string]$FilePath,
        [string]$SearchText
    )
    
    if (Test-Path $FilePath) {
        $lines = Get-Content $FilePath
        $filteredLines = $lines | Where-Object { $_ -notmatch [regex]::Escape($SearchText) }
        Set-Content $FilePath $filteredLines
        Write-Host "✅ Removed lines containing '$SearchText' from: $FilePath" -ForegroundColor Green
    }
}

Write-Host "📝 Fixing major compilation errors..." -ForegroundColor Cyan

# 1. Fix TrainerController - remove GetClassScheduleAsync calls
$trainerController = "$projectPath\Controllers\TrainerController.cs"
Replace-InFile $trainerController "await _lopHocService.GetClassScheduleAsync(classId, start, end)" "new List<object>()" 

# 2. Fix BookingController - remove LichLop references
$bookingController = "$projectPath\Controllers\BookingController.cs"
Replace-InFile $bookingController ".LichLop" ".LopHoc"
Replace-InFile $bookingController "booking.LichLop" "booking.LopHoc"
Replace-InFile $bookingController "lopHoc.LichLops" "new List<object>()"

# 3. Fix HomeController - remove LichLop references  
$homeController = "$projectPath\Controllers\HomeController.cs"
Replace-InFile $homeController ".LichLop" ".LopHoc"
Replace-InFile $homeController "booking.LichLop" "booking.LopHoc"

# 4. Fix DiemDanhRepository
$diemDanhRepo = "$projectPath\Data\Repositories\DiemDanhRepository.cs"
Replace-InFile $diemDanhRepo ".Include(d => d.LichLop)" ""

# 5. Fix BaoCaoService
$baoCaoService = "$projectPath\Services\BaoCaoService.cs"
Replace-InFile $baoCaoService "_context.LichLops" "_context.LopHocs"

# 6. Fix DiemDanhService
$diemDanhService = "$projectPath\Services\DiemDanhService.cs"
Replace-InFile $diemDanhService "_context.LichLops" "_context.LopHocs"

# 7. Fix BookingService
$bookingService = "$projectPath\Services\BookingService.cs"
Replace-InFile $bookingService "_context.LichLops" "_context.LopHocs"

# 8. Fix BangLuongService
$bangLuongService = "$projectPath\Services\BangLuongService.cs"
Replace-InFile $bangLuongService "_context.LichLops" "_context.LopHocs"
Replace-InFile $bangLuongService ".LichLop" ".LopHoc"

Write-Host "🎯 Fixing view compilation errors..." -ForegroundColor Cyan

# 9. Fix Views - remove LichLop references
$views = @(
    "$projectPath\Views\LopHoc\Edit.cshtml",
    "$projectPath\Views\Booking\Index.cshtml", 
    "$projectPath\Views\Booking\MyBookings.cshtml",
    "$projectPath\Views\DiemDanh\Index.cshtml",
    "$projectPath\Views\DiemDanh\AttendanceReport.cshtml",
    "$projectPath\Views\Home\MemberDashboard.cshtml"
)

foreach ($view in $views) {
    if (Test-Path $view) {
        Replace-InFile $view ".LichLop" ".LopHoc"
        Replace-InFile $view "booking.LichLop" "booking.LopHoc"
        Replace-InFile $view "attendance.LichLop" "null"
        Replace-InFile $view "Model.LichLops" "new List<object>()"
    }
}

Write-Host "✅ Compilation error fixes completed!" -ForegroundColor Green
Write-Host "🔄 Run 'dotnet build' to verify fixes..." -ForegroundColor Yellow
