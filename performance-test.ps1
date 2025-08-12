# 🚀 GYM MANAGEMENT SYSTEM - PERFORMANCE TESTING SUITE
# =====================================================
# Load testing and performance validation

Write-Host "🚀 STARTING PERFORMANCE TESTING SUITE" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Yellow

$baseUrl = "http://localhost:5003"
$testResults = @{
    TotalRequests = 0
    SuccessfulRequests = 0
    FailedRequests = 0
    AverageResponseTime = 0
    MaxResponseTime = 0
    MinResponseTime = [int]::MaxValue
}

# Function to test endpoint performance
function Test-EndpointPerformance {
    param(
        [string]$Endpoint,
        [int]$RequestCount = 10,
        [string]$Method = "GET",
        [string]$Description
    )
    
    Write-Host "`n🔧 Testing: $Description" -ForegroundColor Cyan
    Write-Host "Endpoint: $Method $Endpoint" -ForegroundColor Gray
    Write-Host "Requests: $RequestCount" -ForegroundColor Gray
    Write-Host "-" * 50 -ForegroundColor Gray
    
    $responseTimes = @()
    $successCount = 0
    $failCount = 0
    
    for ($i = 1; $i -le $RequestCount; $i++) {
        try {
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            
            if ($Method -eq "GET") {
                $response = Invoke-WebRequest -Uri "$baseUrl$Endpoint" -Method GET -TimeoutSec 30 -ErrorAction Stop
            } else {
                $response = Invoke-WebRequest -Uri "$baseUrl$Endpoint" -Method $Method -TimeoutSec 30 -ErrorAction Stop
            }
            
            $stopwatch.Stop()
            $responseTime = $stopwatch.ElapsedMilliseconds
            $responseTimes += $responseTime
            
            if ($response.StatusCode -eq 200) {
                $successCount++
                Write-Host "  Request $i`: ✅ ${responseTime}ms" -ForegroundColor Green
            } else {
                $failCount++
                Write-Host "  Request $i`: ❌ Status: $($response.StatusCode)" -ForegroundColor Red
            }
            
        } catch {
            $stopwatch.Stop()
            $failCount++
            Write-Host "  Request $i`: ❌ Error: $($_.Exception.Message)" -ForegroundColor Red
        }
        
        # Small delay between requests
        Start-Sleep -Milliseconds 100
    }
    
    # Calculate statistics
    if ($responseTimes.Count -gt 0) {
        $avgTime = ($responseTimes | Measure-Object -Average).Average
        $maxTime = ($responseTimes | Measure-Object -Maximum).Maximum
        $minTime = ($responseTimes | Measure-Object -Minimum).Minimum
        
        Write-Host "`n📊 Results:" -ForegroundColor Yellow
        Write-Host "  ✅ Successful: $successCount/$RequestCount" -ForegroundColor Green
        Write-Host "  ❌ Failed: $failCount/$RequestCount" -ForegroundColor Red
        Write-Host "  ⏱️  Avg Response Time: $([math]::Round($avgTime, 2))ms" -ForegroundColor Cyan
        Write-Host "  ⚡ Min Response Time: ${minTime}ms" -ForegroundColor Green
        Write-Host "  🐌 Max Response Time: ${maxTime}ms" -ForegroundColor Yellow
        
        # Update global results
        $testResults.TotalRequests += $RequestCount
        $testResults.SuccessfulRequests += $successCount
        $testResults.FailedRequests += $failCount
        
        if ($avgTime -gt $testResults.AverageResponseTime) {
            $testResults.AverageResponseTime = $avgTime
        }
        if ($maxTime -gt $testResults.MaxResponseTime) {
            $testResults.MaxResponseTime = $maxTime
        }
        if ($minTime -lt $testResults.MinResponseTime) {
            $testResults.MinResponseTime = $minTime
        }
    }
}

# Function to test concurrent requests
function Test-ConcurrentRequests {
    param(
        [string]$Endpoint,
        [int]$ConcurrentUsers = 5,
        [int]$RequestsPerUser = 3,
        [string]$Description
    )
    
    Write-Host "`n🔥 Concurrent Load Test: $Description" -ForegroundColor Magenta
    Write-Host "Endpoint: $Endpoint" -ForegroundColor Gray
    Write-Host "Concurrent Users: $ConcurrentUsers" -ForegroundColor Gray
    Write-Host "Requests per User: $RequestsPerUser" -ForegroundColor Gray
    Write-Host "-" * 50 -ForegroundColor Gray
    
    $jobs = @()
    $startTime = Get-Date
    
    # Start concurrent jobs
    for ($user = 1; $user -le $ConcurrentUsers; $user++) {
        $job = Start-Job -ScriptBlock {
            param($BaseUrl, $Endpoint, $RequestsPerUser, $UserId)
            
            $results = @{
                UserId = $UserId
                SuccessCount = 0
                FailCount = 0
                ResponseTimes = @()
            }
            
            for ($req = 1; $req -le $RequestsPerUser; $req++) {
                try {
                    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
                    $response = Invoke-WebRequest -Uri "$BaseUrl$Endpoint" -Method GET -TimeoutSec 30 -ErrorAction Stop
                    $stopwatch.Stop()
                    
                    $results.ResponseTimes += $stopwatch.ElapsedMilliseconds
                    
                    if ($response.StatusCode -eq 200) {
                        $results.SuccessCount++
                    } else {
                        $results.FailCount++
                    }
                } catch {
                    $stopwatch.Stop()
                    $results.FailCount++
                }
                
                Start-Sleep -Milliseconds 50
            }
            
            return $results
        } -ArgumentList $baseUrl, $Endpoint, $RequestsPerUser, $user
        
        $jobs += $job
    }
    
    # Wait for all jobs to complete
    Write-Host "  🔄 Running concurrent requests..." -ForegroundColor Yellow
    $jobResults = $jobs | Wait-Job | Receive-Job
    $jobs | Remove-Job
    
    $endTime = Get-Date
    $totalDuration = ($endTime - $startTime).TotalMilliseconds
    
    # Analyze results
    $totalSuccess = ($jobResults | Measure-Object -Property SuccessCount -Sum).Sum
    $totalFail = ($jobResults | Measure-Object -Property FailCount -Sum).Sum
    $allResponseTimes = $jobResults | ForEach-Object { $_.ResponseTimes } | Where-Object { $_ -ne $null }
    
    Write-Host "`n📊 Concurrent Test Results:" -ForegroundColor Yellow
    Write-Host "  👥 Total Users: $ConcurrentUsers" -ForegroundColor Cyan
    Write-Host "  📨 Total Requests: $($totalSuccess + $totalFail)" -ForegroundColor Cyan
    Write-Host "  ✅ Successful: $totalSuccess" -ForegroundColor Green
    Write-Host "  ❌ Failed: $totalFail" -ForegroundColor Red
    Write-Host "  ⏱️  Total Duration: $([math]::Round($totalDuration, 2))ms" -ForegroundColor Yellow
    
    if ($allResponseTimes.Count -gt 0) {
        $avgResponseTime = ($allResponseTimes | Measure-Object -Average).Average
        $maxResponseTime = ($allResponseTimes | Measure-Object -Maximum).Maximum
        $minResponseTime = ($allResponseTimes | Measure-Object -Minimum).Minimum
        
        Write-Host "  ⚡ Avg Response Time: $([math]::Round($avgResponseTime, 2))ms" -ForegroundColor Cyan
        Write-Host "  🚀 Min Response Time: ${minResponseTime}ms" -ForegroundColor Green
        Write-Host "  🐌 Max Response Time: ${maxResponseTime}ms" -ForegroundColor Yellow
        Write-Host "  📈 Requests/Second: $([math]::Round(($totalSuccess + $totalFail) / ($totalDuration / 1000), 2))" -ForegroundColor Magenta
    }
}

# Function to check if application is running
function Test-ApplicationHealth {
    Write-Host "`n🏥 Application Health Check" -ForegroundColor Magenta
    Write-Host "-" * 30 -ForegroundColor Gray
    
    try {
        $response = Invoke-WebRequest -Uri $baseUrl -Method GET -TimeoutSec 10 -ErrorAction Stop
        if ($response.StatusCode -eq 200) {
            Write-Host "  ✅ Application is running on $baseUrl" -ForegroundColor Green
            return $true
        } else {
            Write-Host "  ❌ Application returned status: $($response.StatusCode)" -ForegroundColor Red
            return $false
        }
    } catch {
        Write-Host "  ❌ Application is not accessible: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

# Main execution
$startTime = Get-Date

# Check if application is running
if (-not (Test-ApplicationHealth)) {
    Write-Host "`n❌ Cannot proceed with performance testing. Please ensure the application is running on $baseUrl" -ForegroundColor Red
    exit 1
}

Write-Host "`n🎯 STARTING PERFORMANCE TEST SUITE" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Yellow

# Test 1: Home Page Performance
Test-EndpointPerformance -Endpoint "/" -RequestCount 10 -Description "Home Page Load Test"

# Test 2: Package List Performance
Test-EndpointPerformance -Endpoint "/GoiTap" -RequestCount 15 -Description "Package List Performance"

# Test 3: User Management Performance
Test-EndpointPerformance -Endpoint "/NguoiDung" -RequestCount 10 -Description "User Management Performance"

# Test 4: API Endpoint Performance
Test-EndpointPerformance -Endpoint "/api/GoiTap" -RequestCount 20 -Description "API Endpoint Performance"

# Test 5: Concurrent Load Test
Test-ConcurrentRequests -Endpoint "/" -ConcurrentUsers 5 -RequestsPerUser 3 -Description "Home Page Concurrent Load"

# Test 6: API Concurrent Load Test
Test-ConcurrentRequests -Endpoint "/api/GoiTap" -ConcurrentUsers 3 -RequestsPerUser 5 -Description "API Concurrent Load"

$endTime = Get-Date
$totalDuration = $endTime - $startTime

# Generate final performance report
Write-Host "`n" + "=" * 60 -ForegroundColor Yellow
Write-Host "📊 COMPREHENSIVE PERFORMANCE TEST REPORT" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Yellow

Write-Host "`n🎯 OVERALL RESULTS:" -ForegroundColor Cyan
Write-Host "  📨 Total Requests: $($testResults.TotalRequests)" -ForegroundColor White
Write-Host "  ✅ Successful: $($testResults.SuccessfulRequests)" -ForegroundColor Green
Write-Host "  ❌ Failed: $($testResults.FailedRequests)" -ForegroundColor Red
Write-Host "  ⏱️  Test Duration: $($totalDuration.ToString('mm\:ss'))" -ForegroundColor Yellow

$successRate = if ($testResults.TotalRequests -gt 0) { 
    ($testResults.SuccessfulRequests / $testResults.TotalRequests) * 100 
} else { 0 }
Write-Host "  📈 Success Rate: $($successRate.ToString('F1'))%" -ForegroundColor Cyan

Write-Host "`n⚡ PERFORMANCE METRICS:" -ForegroundColor Magenta
Write-Host "  🚀 Min Response Time: $($testResults.MinResponseTime)ms" -ForegroundColor Green
Write-Host "  ⚡ Avg Response Time: $([math]::Round($testResults.AverageResponseTime, 2))ms" -ForegroundColor Cyan
Write-Host "  🐌 Max Response Time: $($testResults.MaxResponseTime)ms" -ForegroundColor Yellow

Write-Host "`n🎯 PERFORMANCE ASSESSMENT:" -ForegroundColor Cyan
if ($testResults.AverageResponseTime -lt 200) {
    Write-Host "  🎉 EXCELLENT: Average response time < 200ms" -ForegroundColor Green
} elseif ($testResults.AverageResponseTime -lt 500) {
    Write-Host "  ✅ GOOD: Average response time < 500ms" -ForegroundColor Yellow
} else {
    Write-Host "  ⚠️  NEEDS IMPROVEMENT: Average response time > 500ms" -ForegroundColor Red
}

if ($successRate -ge 95) {
    Write-Host "  🎉 EXCELLENT: Success rate >= 95%" -ForegroundColor Green
} elseif ($successRate -ge 90) {
    Write-Host "  ✅ GOOD: Success rate >= 90%" -ForegroundColor Yellow
} else {
    Write-Host "  ⚠️  NEEDS IMPROVEMENT: Success rate < 90%" -ForegroundColor Red
}

Write-Host "`n🏁 Performance testing completed!" -ForegroundColor Green
Write-Host "=" * 60 -ForegroundColor Yellow
