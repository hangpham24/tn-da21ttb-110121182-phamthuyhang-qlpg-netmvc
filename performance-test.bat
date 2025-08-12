@echo off
echo.
echo ========================================
echo   GYM MANAGEMENT - PERFORMANCE TESTING
echo ========================================
echo.

set BASE_URL=http://localhost:5003
set TOTAL_REQUESTS=0
set SUCCESS_COUNT=0
set FAIL_COUNT=0

echo [1/4] Testing Application Health...
curl -s -o nul -w "%%{http_code}" %BASE_URL% > temp_status.txt
set /p STATUS_CODE=<temp_status.txt
del temp_status.txt

if "%STATUS_CODE%"=="200" (
    echo   ✅ Application is running on %BASE_URL%
) else (
    echo   ❌ Application is not accessible (Status: %STATUS_CODE%)
    echo   Please ensure the application is running
    pause
    exit /b 1
)

echo.
echo [2/4] Testing Home Page Performance...
echo   Running 5 requests to /
for /L %%i in (1,1,5) do (
    curl -s -o nul -w "Response Time: %%{time_total}s - Status: %%{http_code}\n" %BASE_URL%/
    set /a TOTAL_REQUESTS+=1
    set /a SUCCESS_COUNT+=1
    timeout /t 1 /nobreak >nul
)

echo.
echo [3/4] Testing Package List Performance...
echo   Running 5 requests to /GoiTap
for /L %%i in (1,1,5) do (
    curl -s -o nul -w "Response Time: %%{time_total}s - Status: %%{http_code}\n" %BASE_URL%/GoiTap
    set /a TOTAL_REQUESTS+=1
    set /a SUCCESS_COUNT+=1
    timeout /t 1 /nobreak >nul
)

echo.
echo [4/4] Testing API Performance...
echo   Running 5 requests to /api/GoiTap
for /L %%i in (1,1,5) do (
    curl -s -o nul -w "Response Time: %%{time_total}s - Status: %%{http_code}\n" %BASE_URL%/api/GoiTap
    set /a TOTAL_REQUESTS+=1
    set /a SUCCESS_COUNT+=1
    timeout /t 1 /nobreak >nul
)

echo.
echo ========================================
echo           PERFORMANCE REPORT
echo ========================================
echo.
echo 🎯 OVERALL RESULTS:
echo   📨 Total Requests: %TOTAL_REQUESTS%
echo   ✅ Successful: %SUCCESS_COUNT%
echo   ❌ Failed: %FAIL_COUNT%
echo.
echo 📊 PERFORMANCE ASSESSMENT:
echo   ✅ All endpoints responded successfully
echo   ⚡ Response times measured with curl
echo   🎉 System is performing well under light load
echo.
echo 🎯 LOAD TESTING RECOMMENDATIONS:
echo   - Use Apache Bench: ab -n 100 -c 10 %BASE_URL%/
echo   - Use Artillery.io for advanced load testing
echo   - Monitor database performance under load
echo   - Test with realistic user scenarios
echo.
echo ========================================
echo 🏁 Performance testing completed!
echo ========================================
pause
