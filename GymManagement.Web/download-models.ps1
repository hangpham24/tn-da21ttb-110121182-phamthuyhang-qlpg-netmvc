# Face-API.js Models Auto Downloader
# Tự động tải tất cả models cần thiết cho Face Recognition

Write-Host "🤖 Face-API.js Models Auto Downloader" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan

# Đường dẫn thư mục models
$modelsPath = "wwwroot/lib/face-api/models"

# Tạo thư mục nếu chưa có
if (!(Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath -Force | Out-Null
    Write-Host "✅ Created models directory: $modelsPath" -ForegroundColor Green
}

# Base URL GitHub - theo Face-API.js official repository
$baseUrl = "https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights"

# Danh sách tất cả model files cần thiết
$models = @{
    "TinyFaceDetector" = @(
        "tiny_face_detector_model-weights_manifest.json",
        "tiny_face_detector_model-shard1"
    )
    "FaceLandmark68Net" = @(
        "face_landmark_68_model-weights_manifest.json", 
        "face_landmark_68_model-shard1"
    )
    "FaceRecognitionNet" = @(
        "face_recognition_model-weights_manifest.json",
        "face_recognition_model-shard1",
        "face_recognition_model-shard2"
    )
}

$totalFiles = 0
$downloadedFiles = 0
$skippedFiles = 0
$failedFiles = 0

# Đếm tổng số files
foreach ($modelType in $models.Keys) {
    $totalFiles += $models[$modelType].Count
}

Write-Host "📦 Total files to download: $totalFiles" -ForegroundColor Yellow
Write-Host ""

# Download từng model
foreach ($modelType in $models.Keys) {
    Write-Host "🔄 Downloading $modelType..." -ForegroundColor Magenta
    
    foreach ($file in $models[$modelType]) {
        $url = "$baseUrl/$file"
        $destination = Join-Path $modelsPath $file
        
        Write-Host "  📄 $file" -NoNewline
        
        try {
            # Kiểm tra file đã tồn tại
            if (Test-Path $destination) {
                Write-Host " - Already exists ✅" -ForegroundColor Yellow
                $skippedFiles++
                continue
            }
            
            # Download file
            $webClient = New-Object System.Net.WebClient
            $webClient.DownloadFile($url, $destination)
            
            # Kiểm tra download thành công
            if (Test-Path $destination) {
                $fileSize = (Get-Item $destination).Length
                $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
                Write-Host " - Downloaded ($fileSizeMB MB) ✅" -ForegroundColor Green
                $downloadedFiles++
            } else {
                Write-Host " - Failed ❌" -ForegroundColor Red
                $failedFiles++
            }
            
            $webClient.Dispose()
        }
        catch {
            Write-Host " - Error: $($_.Exception.Message) ❌" -ForegroundColor Red
            $failedFiles++
        }
    }
    Write-Host ""
}

# Tóm tắt kết quả
Write-Host "📊 Download Summary:" -ForegroundColor Cyan
Write-Host "===================" -ForegroundColor Cyan
Write-Host "Total files: $totalFiles" -ForegroundColor White
Write-Host "Downloaded: $downloadedFiles" -ForegroundColor Green  
Write-Host "Skipped: $skippedFiles" -ForegroundColor Yellow
Write-Host "Failed: $failedFiles" -ForegroundColor Red

# Kiểm tra kết quả
if ($failedFiles -eq 0) {
    Write-Host ""
    Write-Host "🎉 All models downloaded successfully!" -ForegroundColor Green
    Write-Host "🚀 Face Recognition system is ready!" -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "⚠️  Some downloads failed. Please check internet connection." -ForegroundColor Yellow
}

# Liệt kê files đã có
Write-Host ""
Write-Host "📁 Files in models directory:" -ForegroundColor Cyan
if (Test-Path $modelsPath) {
    Get-ChildItem $modelsPath -File | Sort-Object Name | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 1)
        Write-Host "  ✓ $($_.Name) ($size KB)" -ForegroundColor White
    }
} else {
    Write-Host "  ❌ Models directory not found!" -ForegroundColor Red
}

Write-Host ""
Write-Host "🎯 Next Steps:" -ForegroundColor Cyan
Write-Host "1. Start the application: dotnet run" -ForegroundColor White
Write-Host "2. Navigate to: https://localhost:5003/FaceTest" -ForegroundColor White  
Write-Host "3. Click 'Start Camera' to test face detection" -ForegroundColor White

Write-Host ""
Write-Host "✨ Script completed!" -ForegroundColor Green
