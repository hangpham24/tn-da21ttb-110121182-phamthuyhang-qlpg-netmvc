# Download Face-API.js Models Script
# This script downloads the required Face-API.js model files

param(
    [string]$ModelsPath = "wwwroot/lib/face-api/models"
)

Write-Host "Face-API.js Models Downloader" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan

# Ensure models directory exists
if (!(Test-Path $ModelsPath)) {
    New-Item -ItemType Directory -Path $ModelsPath -Force
    Write-Host "Created models directory: $ModelsPath" -ForegroundColor Green
}

# Base URL for Face-API.js models
$baseUrl = "https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights"

# Required model files
$modelFiles = @(
    "tiny_face_detector_model-weights_manifest.json",
    "tiny_face_detector_model-shard1",
    "face_landmark_68_model-weights_manifest.json",
    "face_landmark_68_model-shard1",
    "face_recognition_model-weights_manifest.json",
    "face_recognition_model-shard1",
    "face_recognition_model-shard2"
)

Write-Host "Downloading Face-API.js models..." -ForegroundColor Yellow

$totalFiles = $modelFiles.Count
$downloadedFiles = 0
$failedFiles = 0

foreach ($file in $modelFiles) {
    $url = "$baseUrl/$file"
    $destination = Join-Path $ModelsPath $file

    Write-Host "Downloading: $file" -ForegroundColor White

    try {
        # Check if file already exists
        if (Test-Path $destination) {
            Write-Host "  File already exists, skipping..." -ForegroundColor Yellow
            $downloadedFiles++
            continue
        }

        # Download with progress
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($url, $destination)

        # Verify download
        if (Test-Path $destination) {
            $fileSize = (Get-Item $destination).Length
            $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
            Write-Host "  Downloaded successfully ($fileSizeMB MB)" -ForegroundColor Green
            $downloadedFiles++
        } else {
            Write-Host "  Download failed - file not found" -ForegroundColor Red
            $failedFiles++
        }

        $webClient.Dispose()
    }
    catch {
        Write-Host "  Download failed: $($_.Exception.Message)" -ForegroundColor Red
        $failedFiles++
    }
}

Write-Host ""
Write-Host "Download Summary:" -ForegroundColor Cyan
Write-Host "================" -ForegroundColor Cyan
Write-Host "Total files: $totalFiles" -ForegroundColor White
Write-Host "Downloaded: $downloadedFiles" -ForegroundColor Green
Write-Host "Failed: $failedFiles" -ForegroundColor Red

if ($failedFiles -eq 0) {
    Write-Host ""
    Write-Host "All Face-API.js models downloaded successfully!" -ForegroundColor Green
    Write-Host "The face recognition system is now ready to use." -ForegroundColor Green
} else {
    Write-Host ""
    Write-Host "Some downloads failed. Please check your internet connection and try again." -ForegroundColor Yellow
}

# List downloaded files
Write-Host ""
Write-Host "Files in models directory:" -ForegroundColor Cyan
Get-ChildItem $ModelsPath | ForEach-Object {
    $size = [math]::Round($_.Length / 1KB, 1)
    Write-Host "  $($_.Name) ($size KB)" -ForegroundColor White
}

Write-Host ""
Write-Host "Usage Instructions:" -ForegroundColor Cyan
Write-Host "==================" -ForegroundColor Cyan
Write-Host "1. Navigate to http://localhost:5003/FaceTest" -ForegroundColor White
Write-Host "2. Click 'Start Camera' to begin face detection" -ForegroundColor White
Write-Host "3. The models will load automatically" -ForegroundColor White

Write-Host ""
Write-Host "Script completed!" -ForegroundColor Green
