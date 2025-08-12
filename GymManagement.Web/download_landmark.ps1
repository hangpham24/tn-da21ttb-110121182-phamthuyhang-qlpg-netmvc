# Download Face Landmark 68 Model
Write-Host "Downloading Face Landmark 68 Model..." -ForegroundColor Cyan

$baseUrl = "https://raw.githubusercontent.com/justadudewhohacks/face-api.js/master/weights"
$modelsPath = "wwwroot/lib/face-api/models"

# Ensure directory exists
if (!(Test-Path $modelsPath)) {
    New-Item -ItemType Directory -Path $modelsPath -Force
}

# Files to download
$files = @(
    "face_landmark_68_model-weights_manifest.json",
    "face_landmark_68_model-shard1"
)

foreach ($file in $files) {
    $url = "$baseUrl/$file"
    $destination = Join-Path $modelsPath $file
    
    Write-Host "Downloading: $file" -ForegroundColor White
    
    try {
        $webClient = New-Object System.Net.WebClient
        $webClient.DownloadFile($url, $destination)
        
        if (Test-Path $destination) {
            $fileSize = (Get-Item $destination).Length
            $fileSizeMB = [math]::Round($fileSize / 1MB, 2)
            Write-Host "  Downloaded successfully ($fileSizeMB MB)" -ForegroundColor Green
        }
        
        $webClient.Dispose()
    }
    catch {
        Write-Host "  Download failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "Download completed!" -ForegroundColor Green
