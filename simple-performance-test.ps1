Write-Host "🚀 GYM MANAGEMENT SYSTEM - PERFORMANCE TESTING" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Yellow

$baseUrl = "http://localhost:5003"
$totalRequests = 0
$successfulRequests = 0
$failedRequests = 0
$responseTimes = @()

# Function to test endpoint
function Test-Endpoint {
    param(
        [string]$Endpoint,
        [int]$RequestCount,
        [string]$Description
    )
    
    Write-Host "`n🔧 Testing: $Description" -ForegroundColor Cyan
    Write-Host "Endpoint: $Endpoint" -ForegroundColor Gray
    Write-Host "Requests: $RequestCount" -ForegroundColor Gray
    Write-Host "-" * 30 -ForegroundColor Gray
    
    $localSuccess = 0
    $localFail = 0
    $localTimes = @()
    
    for ($i = 1; $i -le $RequestCount; $i++) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $response = Invoke-WebRequest -Uri "$baseUrl$Endpoint" -Method GET -TimeoutSec 10 -ErrorAction Stop
            $stopwatch.Stop()
            
            $responseTime = $stopwatch.ElapsedMilliseconds
            $localTimes += $responseTime
            
            if ($response.StatusCode -eq 200) {
                $localSuccess++
                Write-Host "  Request $i : ✅ ${responseTime}ms" -ForegroundColor Green
            } else {
                $localFail++
                Write-Host "  Request $i : ❌ Status: $($response.StatusCode)" -ForegroundColor Red
            }
        } catch {
            $localFail++
            Write-Host "  Request $i : ❌ Error" -ForegroundColor Red
        }
        
        Start-Sleep -Milliseconds 200
    }
    
    # Update global counters
    $script:totalRequests += $RequestCount
    $script:successfulRequests += $localSuccess
    $script:failedRequests += $localFail
    $script:responseTimes += $localTimes
    
    # Show local results
    if ($localTimes.Count -gt 0) {
        $avgTime = ($localTimes | Measure-Object -Average).Average
        $maxTime = ($localTimes | Measure-Object -Maximum).Maximum
        $minTime = ($localTimes | Measure-Object -Minimum).Minimum
        
        Write-Host "`n📊 Results:" -ForegroundColor Yellow
        Write-Host "  ✅ Successful: $localSuccess/$RequestCount" -ForegroundColor Green
        Write-Host "  ❌ Failed: $localFail/$RequestCount" -ForegroundColor Red
        Write-Host "  ⏱️ Avg Time: $([math]::Round($avgTime, 2))ms" -ForegroundColor Cyan
        Write-Host "  ⚡ Min Time: ${minTime}ms" -ForegroundColor Green
        Write-Host "  🐌 Max Time: ${maxTime}ms" -ForegroundColor Yellow
    }
}

# Check if application is running
Write-Host "`n🏥 Application Health Check" -ForegroundColor Magenta
try {
    $response = Invoke-WebRequest -Uri $baseUrl -Method GET -TimeoutSec 5 -ErrorAction Stop
    if ($response.StatusCode -eq 200) {
        Write-Host "  ✅ Application is running on $baseUrl" -ForegroundColor Green
    } else {
        Write-Host "  ❌ Application returned status: $($response.StatusCode)" -ForegroundColor Red
        exit 1
    }
} catch {
    Write-Host "  ❌ Application is not accessible" -ForegroundColor Red
    Write-Host "  Please ensure the application is running on $baseUrl" -ForegroundColor Yellow
    exit 1
}

$startTime = Get-Date

Write-Host "`n🎯 STARTING PERFORMANCE TESTS" -ForegroundColor Green
Write-Host "=" * 40 -ForegroundColor Yellow

# Test 1: Home Page
Test-Endpoint -Endpoint "/" -RequestCount 5 -Description "Home Page Load Test"

# Test 2: Package List
Test-Endpoint -Endpoint "/GoiTap" -RequestCount 5 -Description "Package List Performance"

# Test 3: User Management
Test-Endpoint -Endpoint "/NguoiDung" -RequestCount 3 -Description "User Management Performance"

# Test 4: API Endpoint
Test-Endpoint -Endpoint "/api/GoiTap" -RequestCount 5 -Description "API Endpoint Performance"

$endTime = Get-Date
$totalDuration = $endTime - $startTime

# Generate final report
Write-Host "`n" + "=" * 50 -ForegroundColor Yellow
Write-Host "📊 PERFORMANCE TEST REPORT" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Yellow

Write-Host "`n🎯 OVERALL RESULTS:" -ForegroundColor Cyan
Write-Host "  📨 Total Requests: $totalRequests" -ForegroundColor White
Write-Host "  ✅ Successful: $successfulRequests" -ForegroundColor Green
Write-Host "  ❌ Failed: $failedRequests" -ForegroundColor Red
Write-Host "  ⏱️ Test Duration: $($totalDuration.ToString('mm\:ss'))" -ForegroundColor Yellow

$successRate = if ($totalRequests -gt 0) { 
    ($successfulRequests / $totalRequests) * 100 
} else { 0 }
Write-Host "  📈 Success Rate: $($successRate.ToString('F1'))%" -ForegroundColor Cyan

if ($responseTimes.Count -gt 0) {
    $avgResponseTime = ($responseTimes | Measure-Object -Average).Average
    $maxResponseTime = ($responseTimes | Measure-Object -Maximum).Maximum
    $minResponseTime = ($responseTimes | Measure-Object -Minimum).Minimum
    
    Write-Host "`n⚡ PERFORMANCE METRICS:" -ForegroundColor Magenta
    Write-Host "  🚀 Min Response Time: ${minResponseTime}ms" -ForegroundColor Green
    Write-Host "  ⚡ Avg Response Time: $([math]::Round($avgResponseTime, 2))ms" -ForegroundColor Cyan
    Write-Host "  🐌 Max Response Time: ${maxResponseTime}ms" -ForegroundColor Yellow
    
    Write-Host "`n🎯 PERFORMANCE ASSESSMENT:" -ForegroundColor Cyan
    if ($avgResponseTime -lt 200) {
        Write-Host "  🎉 EXCELLENT: Average response time under 200ms" -ForegroundColor Green
    } elseif ($avgResponseTime -lt 500) {
        Write-Host "  ✅ GOOD: Average response time under 500ms" -ForegroundColor Yellow
    } else {
        Write-Host "  ⚠️ NEEDS IMPROVEMENT: Average response time over 500ms" -ForegroundColor Red
    }
    
    if ($successRate -ge 95) {
        Write-Host "  🎉 EXCELLENT: Success rate 95% or higher" -ForegroundColor Green
    } elseif ($successRate -ge 90) {
        Write-Host "  ✅ GOOD: Success rate 90% or higher" -ForegroundColor Yellow
    } else {
        Write-Host "  ⚠️ NEEDS IMPROVEMENT: Success rate below 90%" -ForegroundColor Red
    }
}

Write-Host "`n🏁 Performance testing completed!" -ForegroundColor Green
Write-Host "=" * 50 -ForegroundColor Yellow
