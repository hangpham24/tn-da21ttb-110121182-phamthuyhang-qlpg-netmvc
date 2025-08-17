# 🔧 ENTITY FRAMEWORK CODE CHANGES

## A. Model Files to Delete

### Files to Remove:
```bash
# Delete these model files
rm "GymManagement.Web/Data/Models/LichLop.cs"
rm "GymManagement.Web/Data/Models/KhuyenMaiUsage.cs"
```

## B. DbContext Changes

### File: `GymManagement.Web/Data/GymDbContext.cs`

**Lines to Remove:**
- Line 24: `public DbSet<LichLop> LichLops { get; set; }`
- Line 26: `public DbSet<KhuyenMaiUsage> KhuyenMaiUsages { get; set; }`

**Configuration to Remove:**
- All `modelBuilder.Entity<LichLop>()` configurations
- All `modelBuilder.Entity<KhuyenMaiUsage>()` configurations
- Line 262-264: LichLop foreign key configuration in Booking entity

## C. Navigation Properties Updates

### File: `GymManagement.Web/Data/Models/LopHoc.cs`
**Remove:**
- Line 62: `public virtual ICollection<LichLop> LichLops { get; set; } = new List<LichLop>();`

### File: `GymManagement.Web/Data/Models/Booking.cs`
**Remove:**
- Line 31: `public virtual LichLop? LichLop { get; set; }`

### File: `GymManagement.Web/Data/Models/DiemDanh.cs`
**Remove:**
- Line 38: `public virtual LichLop? LichLop { get; set; }`

## D. UnitOfWork Changes

### File: `GymManagement.Web/Data/UnitOfWork.cs`
**Remove:**
- Line 17: `private IRepository<Models.LichLop>? _lichLops;`
- Lines 55-56: LichLops property getter

### File: `GymManagement.Web/Data/IUnitOfWork.cs`
**Remove:**
- Line 16: `IRepository<Models.LichLop> LichLops { get; }`

## E. Repository Changes

### File: `GymManagement.Web/Data/Repositories/ILopHocRepository.cs`
**Remove:**
- Line 12: `Task<LopHoc?> GetWithLichLopsAsync(int lopHocId);`

### File: `GymManagement.Web/Data/Repositories/LopHocRepository.cs`
**Remove:**
- Lines 145-150: `GetWithLichLopsAsync` method implementation
- Any `.Include(x => x.LichLops)` statements

# 🧹 SERVICE LAYER CLEANUP

## A. Interface Methods to Remove

### File: `GymManagement.Web/Services/IKhuyenMaiService.cs`
**Remove these methods:**
```csharp
Task<bool> TrackUsageAsync(int khuyenMaiId, int nguoiDungId, decimal soTienGoc, decimal soTienGiam, decimal soTienCuoi, int? thanhToanId = null, int? dangKyId = null, string? ghiChu = null);
Task<IEnumerable<KhuyenMaiUsage>> GetUsageHistoryAsync(int khuyenMaiId);
Task<IEnumerable<KhuyenMaiUsage>> GetUserUsageHistoryAsync(int nguoiDungId);
Task<int> GetUsageCountAsync(int khuyenMaiId);
Task<decimal> GetTotalDiscountAmountAsync(int khuyenMaiId);
```

### File: `GymManagement.Web/Services/ILopHocService.cs`
**Remove these methods:**
```csharp
Task GenerateScheduleAsync(int lopHocId, DateTime startDate, DateTime endDate);
Task<IEnumerable<LichLop>> GetClassScheduleAsync(int lopHocId, DateTime startDate, DateTime endDate);
Task<bool> CancelClassAsync(int lichLopId, string reason);
```

## B. Service Implementation Cleanup

### File: `GymManagement.Web/Services/KhuyenMaiService.cs`
**Remove these method implementations:**
- `TrackUsageAsync()` (lines 303-332)
- `GetUsageHistoryAsync()` (lines 334-344)
- `GetUserUsageHistoryAsync()` (lines 346-356)
- `GetUsageCountAsync()` (lines 358-368)
- `GetTotalDiscountAmountAsync()` (lines 370-380)

### File: `GymManagement.Web/Services/LopHocService.cs`
**Remove these method implementations:**
- `GenerateScheduleAsync()` method
- `GetClassScheduleAsync()` method  
- `CancelClassAsync()` method
- Any references to `_unitOfWork.Context.LichLops`

# 🎮 CONTROLLER AND API CLEANUP

## A. Controller Actions to Remove

### File: `GymManagement.Web/Controllers/LopHocController.cs`
**Remove these actions:**
- `GenerateSchedule()` action
- `GetSchedule()` action (lines 522-542)
- Any JavaScript functions calling schedule generation

### File: `GymManagement.Web/Areas/Api/Controllers/KhuyenMaiController.cs`
**Remove entire controller if it exists**

## B. View Files to Update

### File: `GymManagement.Web/Views/LopHoc/Details.cshtml`
**Remove:**
- Schedule generation button
- JavaScript `generateSchedule()` function
- Any references to LichLop data

### File: `GymManagement.Web/Views/Trainer/Attendance.cshtml`
**Update:**
- Remove references to `LichLop` model (line 7)
- Update attendance logic to work without LichLop

## C. DTO Classes to Update

### File: `GymManagement.Web/Models/DTOs/LopHocDto.cs`
**Remove:**
- `LichLopDto` class (lines 102-119)
- Any properties referencing LichLop

# 🧪 TESTING CLEANUP

## A. Test Files to Update

### File: `GymManagement.Tests/Unit/Services/LopHocServiceTests.cs`
**Remove:**
- Tests for `GenerateScheduleAsync()`
- Tests for `GetClassScheduleAsync()`
- Tests for `CancelClassAsync()`

### File: `GymManagement.Tests/Unit/Services/KhuyenMaiServiceTests.cs`
**Remove:**
- Tests for `TrackUsageAsync()`
- Tests for usage history methods
- Any mock setups for KhuyenMaiUsage

### File: `GymManagement.Tests/Performance/ConcurrentBookingTests.cs`
**Update:**
- Remove any references to LichLop in test data seeding
- Update booking logic to work without LichLop

## B. Integration Test Updates

### File: `GymManagement.Tests/Integration/BookingAuthorizationIntegrationTests.cs`
**Update:**
- Remove LichLop from test data setup
- Update booking assertions

# 📋 EXECUTION CHECKLIST

## Phase 1: Preparation (30 minutes)
- [ ] Create database backup
- [ ] Run dependency analysis script
- [ ] Verify tables are empty
- [ ] Create rollback plan

## Phase 2: Database Changes (15 minutes)
- [ ] Run SQL cleanup script
- [ ] Verify tables are dropped
- [ ] Check foreign key constraints removed
- [ ] Verify data integrity

## Phase 3: Code Changes (45 minutes)
- [ ] Delete model files
- [ ] Update DbContext
- [ ] Remove navigation properties
- [ ] Update UnitOfWork and repositories
- [ ] Clean up service interfaces and implementations

## Phase 4: Controller and View Updates (30 minutes)
- [ ] Remove controller actions
- [ ] Update view files
- [ ] Remove JavaScript functions
- [ ] Update DTOs

## Phase 5: Testing (30 minutes)
- [ ] Update test files
- [ ] Remove obsolete tests
- [ ] Run test suite
- [ ] Verify no compilation errors

## Phase 6: Verification (15 minutes)
- [ ] Build solution successfully
- [ ] Run application
- [ ] Test core functionality
- [ ] Verify no runtime errors

**Total Estimated Time: 2.5 hours**
