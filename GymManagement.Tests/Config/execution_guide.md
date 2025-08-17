# 📋 STEP-BY-STEP EXECUTION GUIDE

## 🚨 PRE-EXECUTION CHECKLIST

### ✅ Prerequisites
- [ ] Visual Studio 2022 or VS Code installed
- [ ] .NET 8.0 SDK installed
- [ ] SQL Server Management Studio (optional)
- [ ] Git repository is clean (no uncommitted changes)
- [ ] Application is currently running without errors
- [ ] Database backup location exists: `C:\Backup\`

### ⚠️ Safety Measures
- [ ] Create Git branch: `git checkout -b cleanup-lichlops-khuyenmaiusages`
- [ ] Inform team members about maintenance window
- [ ] Ensure no active users in the system
- [ ] Have rollback plan ready

---

## 🎯 PHASE 1: PREPARATION (30 minutes)

### Step 1.1: Create Database Backup
```powershell
# Open SQL Server Management Studio or run via sqlcmd
sqlcmd -S "(localdb)\mssqllocaldb" -d "GymManagementDb" -Q "
BACKUP DATABASE [GymManagementDb] 
TO DISK = 'C:\Backup\GymManagementDb_BeforeCleanup_$(Get-Date -Format 'yyyyMMdd_HHmmss').bak'
WITH FORMAT, INIT, COMPRESSION;
"
```

### Step 1.2: Verify Current State
```powershell
# Check if tables are empty (should be)
sqlcmd -S "(localdb)\mssqllocaldb" -d "GymManagementDb" -Q "
SELECT 'LichLops' as TableName, COUNT(*) as RecordCount FROM LichLops
UNION ALL
SELECT 'KhuyenMaiUsages' as TableName, COUNT(*) as RecordCount FROM KhuyenMaiUsages
"
```

### Step 1.3: Run Dependency Analysis
```powershell
# Execute the cleanup_plan.sql (Steps 1-2 only)
sqlcmd -S "(localdb)\mssqllocaldb" -d "GymManagementDb" -i "cleanup_plan.sql"
```

---

## 🗄️ PHASE 2: DATABASE CHANGES (15 minutes)

### Step 2.1: Execute Database Cleanup
```powershell
# Run the complete cleanup script
sqlcmd -S "(localdb)\mssqllocaldb" -d "GymManagementDb" -i "cleanup_plan.sql"
```

### Step 2.2: Verify Database Changes
```powershell
# Verify tables are dropped
sqlcmd -S "(localdb)\mssqllocaldb" -d "GymManagementDb" -Q "
SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_NAME IN ('LichLops', 'KhuyenMaiUsages')
"
# Should return no results
```

---

## 💻 PHASE 3: CODE CHANGES (45 minutes)

### Step 3.1: Delete Model Files
```powershell
# Navigate to project directory
cd "GymManagement.Web"

# Delete model files
Remove-Item "Data\Models\LichLop.cs" -Force
Remove-Item "Data\Models\KhuyenMaiUsage.cs" -Force

Write-Host "✅ Model files deleted" -ForegroundColor Green
```

### Step 3.2: Update DbContext
```powershell
# Edit GymDbContext.cs
# Remove these lines:
# - Line 24: public DbSet<LichLop> LichLops { get; set; }
# - Line 26: public DbSet<KhuyenMaiUsage> KhuyenMaiUsages { get; set; }
# - All modelBuilder.Entity<LichLop>() configurations
# - All modelBuilder.Entity<KhuyenMaiUsage>() configurations
```

### Step 3.3: Update Navigation Properties
```powershell
# Edit LopHoc.cs - Remove:
# public virtual ICollection<LichLop> LichLops { get; set; } = new List<LichLop>();

# Edit Booking.cs - Remove:
# public virtual LichLop? LichLop { get; set; }

# Edit DiemDanh.cs - Remove:
# public virtual LichLop? LichLop { get; set; }
```

### Step 3.4: Update UnitOfWork and Repositories
```powershell
# Edit UnitOfWork.cs - Remove:
# - private IRepository<Models.LichLop>? _lichLops;
# - LichLops property getter

# Edit IUnitOfWork.cs - Remove:
# - IRepository<Models.LichLop> LichLops { get; }

# Edit ILopHocRepository.cs - Remove:
# - Task<LopHoc?> GetWithLichLopsAsync(int lopHocId);

# Edit LopHocRepository.cs - Remove:
# - GetWithLichLopsAsync method implementation
```

### Step 3.5: Clean Up Service Interfaces
```powershell
# Edit IKhuyenMaiService.cs - Remove these methods:
# - TrackUsageAsync()
# - GetUsageHistoryAsync()
# - GetUserUsageHistoryAsync()
# - GetUsageCountAsync()
# - GetTotalDiscountAmountAsync()

# Edit ILopHocService.cs - Remove these methods:
# - GenerateScheduleAsync()
# - GetClassScheduleAsync()
# - CancelClassAsync()
```

### Step 3.6: Clean Up Service Implementations
```powershell
# Edit KhuyenMaiService.cs - Remove method implementations:
# - TrackUsageAsync() (lines 303-332)
# - GetUsageHistoryAsync() (lines 334-344)
# - GetUserUsageHistoryAsync() (lines 346-356)
# - GetUsageCountAsync() (lines 358-368)
# - GetTotalDiscountAmountAsync() (lines 370-380)

# Edit LopHocService.cs - Remove method implementations:
# - GenerateScheduleAsync()
# - GetClassScheduleAsync()
# - CancelClassAsync()
# - Any references to _unitOfWork.Context.LichLops
```

---

## 🎮 PHASE 4: CONTROLLER AND VIEW UPDATES (30 minutes)

### Step 4.1: Update Controllers
```powershell
# Edit LopHocController.cs - Remove:
# - GenerateSchedule() action
# - GetSchedule() action (lines 522-542)

# Delete entire controller if exists:
# Areas\Api\Controllers\KhuyenMaiController.cs
```

### Step 4.2: Update Views
```powershell
# Edit Views\LopHoc\Details.cshtml - Remove:
# - Schedule generation button
# - JavaScript generateSchedule() function

# Edit Views\Trainer\Attendance.cshtml - Update:
# - Remove references to LichLop model (line 7)
# - Update attendance logic
```

### Step 4.3: Update DTOs
```powershell
# Edit Models\DTOs\LopHocDto.cs - Remove:
# - LichLopDto class (lines 102-119)
```

---

## 🧪 PHASE 5: TESTING UPDATES (30 minutes)

### Step 5.1: Update Unit Tests
```powershell
# Edit Tests\Unit\Services\LopHocServiceTests.cs - Remove:
# - Tests for GenerateScheduleAsync()
# - Tests for GetClassScheduleAsync()
# - Tests for CancelClassAsync()

# Edit Tests\Unit\Services\KhuyenMaiServiceTests.cs - Remove:
# - Tests for TrackUsageAsync()
# - Tests for usage history methods
```

### Step 5.2: Update Integration Tests
```powershell
# Edit Tests\Integration\BookingAuthorizationIntegrationTests.cs
# - Remove LichLop from test data setup
# - Update booking assertions

# Edit Tests\Performance\ConcurrentBookingTests.cs
# - Remove references to LichLop in test data seeding
```

---

## ✅ PHASE 6: VERIFICATION (15 minutes)

### Step 6.1: Build Solution
```powershell
# Build the solution
dotnet build --configuration Release

# Check for compilation errors
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed - check errors" -ForegroundColor Red
    # If build fails, check cleanup_steps.md for missed items
}
```

### Step 6.2: Run Tests
```powershell
# Run unit tests
dotnet test --configuration Release

# Check test results
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ All tests passed" -ForegroundColor Green
} else {
    Write-Host "❌ Some tests failed - review and fix" -ForegroundColor Red
}
```

### Step 6.3: Create New Migration
```powershell
# Add the cleanup migration
dotnet ef migrations add RemoveLichLopsAndKhuyenMaiUsagesTables

# Update database
dotnet ef database update

Write-Host "✅ Migration applied successfully" -ForegroundColor Green
```

### Step 6.4: Run Application
```powershell
# Start the application
dotnet run --project GymManagement.Web

# Test key functionality:
# 1. Navigate to https://localhost:7000
# 2. Test user login
# 3. Test class management
# 4. Test booking calendar
# 5. Test promotion system
# 6. Verify no errors in console
```

### Step 6.5: Execute Validation Script
```powershell
# Run comprehensive validation
.\validation_tests.ps1

# Review results and address any issues
```

---

## 🎯 POST-EXECUTION TASKS

### Step 7.1: Commit Changes
```powershell
# Stage all changes
git add .

# Commit with descriptive message
git commit -m "feat: Remove unused LichLops and KhuyenMaiUsages tables

- Removed LichLop and KhuyenMaiUsage models
- Updated DbContext and navigation properties  
- Cleaned up service interfaces and implementations
- Removed related controller actions and views
- Updated unit and integration tests
- Applied database migration to drop tables
- Verified system functionality remains intact

Closes #[issue-number]"
```

### Step 7.2: Merge to Main Branch
```powershell
# Switch to main branch
git checkout main

# Merge cleanup branch
git merge cleanup-lichlops-khuyenmaiusages

# Push changes
git push origin main

# Delete cleanup branch
git branch -d cleanup-lichlops-khuyenmaiusages
```

### Step 7.3: Update Documentation
```powershell
# Update README.md to reflect simplified architecture
# Update API documentation
# Update database schema documentation
# Notify team of changes
```

---

## 🚨 EMERGENCY PROCEDURES

### If Build Fails:
1. Check `cleanup_steps.md` for missed items
2. Review compilation errors carefully
3. Restore specific files from git if needed
4. Continue with remaining cleanup steps

### If Tests Fail:
1. Review test failures
2. Update test expectations for removed functionality
3. Fix any remaining references to deleted models
4. Re-run tests until all pass

### If Application Crashes:
1. Check application logs
2. Review runtime errors
3. Verify all references to deleted models are removed
4. Use rollback plan if necessary

### Complete Rollback:
```powershell
# If major issues occur, execute complete rollback
git checkout main
git reset --hard HEAD~1  # Reset to before cleanup
.\rollback_plan.sql      # Restore database
dotnet ef database update  # Apply previous migrations
```

---

## 📊 SUCCESS CRITERIA

✅ **Database:**
- LichLops and KhuyenMaiUsages tables removed
- No orphaned foreign key constraints
- Data integrity maintained

✅ **Code:**
- Solution builds without errors
- All tests pass
- No runtime exceptions

✅ **Functionality:**
- User management works
- Class management works
- Booking system works (without LichLop dependency)
- Payment system works (without usage tracking)
- Promotion validation works

✅ **Performance:**
- Application startup time unchanged or improved
- Page load times unchanged or improved
- Database query performance unchanged or improved

**🎉 Cleanup completed successfully!**
