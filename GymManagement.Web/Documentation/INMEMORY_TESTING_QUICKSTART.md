# 🚀 **IN-MEMORY TESTING QUICK START GUIDE**

## 📋 **OVERVIEW**

This guide helps you quickly migrate from Mock-based tests to In-Memory tests using the new infrastructure.

**Migration Time**: 15-30 minutes per test file  
**Difficulty**: Easy to Medium  
**Benefits**: More realistic tests, better integration coverage, less maintenance

---

## 🛠️ **STEP-BY-STEP MIGRATION**

### **Step 1: Change Base Class**

**Before (Mock-based):**
```csharp
public class MyServiceTests
{
    private readonly Mock<IRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly MyService _service;
    
    public MyServiceTests()
    {
        _mockRepository = new Mock<IRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _service = new MyService(_mockRepository.Object, _mockUnitOfWork.Object);
    }
}
```

**After (In-Memory):**
```csharp
public class MyServiceTests : InMemoryTestBase
{
    // No need for mock declarations!
    // Services are available via base class properties:
    // - MyService (if registered in InMemoryTestBase)
    // - Context (for database operations)
    // - UnitOfWork (real implementation)
}
```

### **Step 2: Replace Mock Setups with Real Data**

**Before (Mock setup):**
```csharp
[Fact]
public async Task GetUser_ShouldReturnUser()
{
    // Arrange
    var expectedUser = new NguoiDung { Id = 1, Ten = "Test User" };
    _mockRepository.Setup(x => x.GetByIdAsync(1))
                   .ReturnsAsync(expectedUser);
    
    // Act
    var result = await _service.GetUserAsync(1);
    
    // Assert
    result.Should().Be(expectedUser);
    _mockRepository.Verify(x => x.GetByIdAsync(1), Times.Once);
}
```

**After (Real data):**
```csharp
[Fact]
public async Task GetUser_ShouldReturnUser()
{
    // Arrange - Create real data in In-Memory database
    var user = new NguoiDung 
    { 
        Ho = "Test", 
        Ten = "User",
        Email = "test@example.com",
        LoaiNguoiDung = "THANHVIEN",
        TrangThai = "ACTIVE"
    };
    Context.NguoiDungs.Add(user);
    await Context.SaveChangesAsync();
    
    // Act - Use real service
    var result = await NguoiDungService.GetByIdAsync(user.Id);
    
    // Assert - Verify real business logic
    result.Should().NotBeNull();
    result.Ten.Should().Be("User");
    result.Email.Should().Be("test@example.com");
}
```

### **Step 3: Replace Mock Verifications with State Assertions**

**Before (Mock verification):**
```csharp
[Fact]
public async Task CreateUser_ShouldCallRepository()
{
    // Arrange
    var newUser = new NguoiDung { Ten = "New User" };
    
    // Act
    await _service.CreateAsync(newUser);
    
    // Assert
    _mockRepository.Verify(x => x.AddAsync(It.IsAny<NguoiDung>()), Times.Once);
    _mockUnitOfWork.Verify(x => x.SaveChangesAsync(), Times.Once);
}
```

**After (State assertion):**
```csharp
[Fact]
public async Task CreateUser_ShouldSaveToDatabase()
{
    // Arrange
    var newUser = new NguoiDung 
    { 
        Ho = "New",
        Ten = "User",
        Email = "newuser@example.com",
        LoaiNguoiDung = "THANHVIEN",
        TrangThai = "ACTIVE"
    };
    
    // Act
    var result = await NguoiDungService.CreateAsync(newUser);
    
    // Assert - Verify actual database state
    result.Should().NotBeNull();
    result.Id.Should().BeGreaterThan(0);
    
    var savedUser = await Context.NguoiDungs.FindAsync(result.Id);
    savedUser.Should().NotBeNull();
    savedUser.Email.Should().Be("newuser@example.com");
    savedUser.NgayTao.Should().BeCloseTo(DateTime.Now, TimeSpan.FromMinutes(1));
}
```

---

## 🎯 **COMMON PATTERNS**

### **Pattern 1: Creating Test Data**

```csharp
// Helper method in your test class
private async Task<NguoiDung> CreateTestUserAsync(string role = "THANHVIEN")
{
    var user = new NguoiDung
    {
        Ho = "Test",
        Ten = $"User {Guid.NewGuid().ToString()[..8]}",
        Email = $"test{Random.Shared.Next(1000, 9999)}@example.com",
        LoaiNguoiDung = role,
        TrangThai = "ACTIVE",
        NgayTao = DateTime.Now
    };
    
    Context.NguoiDungs.Add(user);
    await Context.SaveChangesAsync();
    return user;
}
```

### **Pattern 2: Testing Business Logic with Real Data**

```csharp
[Fact]
public async Task CalculateCommission_WithRealData_ShouldReturnCorrectAmount()
{
    // Arrange - Create complete business scenario
    var trainer = await CreateTestUserAsync("TRAINER");
    var member = await CreateTestUserAsync("THANHVIEN");
    var package = TestPackages.First(p => p.TenGoi == "Gói Premium");
    
    var registration = await CreateTestRegistrationAsync(member.Id, package.Id, "ACTIVE");
    await CreateTestPaymentAsync(registration.Id, package.Gia, "SUCCESS");
    
    // Act - Real business logic
    var commission = await BangLuongService.CalculateMonthlyCommissionAsync(
        trainer.Id, DateTime.Now.Month, DateTime.Now.Year);
    
    // Assert - Verify complex business rules
    commission.Should().NotBeNull();
    commission.PackageCommission.Should().BeGreaterThan(0);
    commission.TotalCommission.Should().Be(
        commission.PackageCommission + commission.ClassCommission + commission.AttendanceBonus);
    
    // Verify database state
    var salaryRecord = await Context.BangLuongs
        .FirstOrDefaultAsync(b => b.NguoiDungId == trainer.Id);
    salaryRecord.Should().NotBeNull();
}
```

### **Pattern 3: Testing External Service Integration**

```csharp
[Fact]
public async Task ProcessPayment_ShouldSendNotificationAndEmail()
{
    // Arrange
    var member = await CreateTestUserAsync("THANHVIEN");
    var package = TestPackages.First();
    var registration = await CreateTestRegistrationAsync(member.Id, package.Id, "PENDING");
    
    // Reset fake services to clear any setup data
    ResetFakeServices();
    
    // Act
    var payment = await ThanhToanService.ProcessCashPaymentAsync(
        registration.Id, package.Gia, "Test payment");
    
    // Assert - Verify external service calls
    payment.TrangThai.Should().Be("SUCCESS");
    
    // Verify fake notification service was called
    FakeNotificationService.WasNotificationSent(member.Id, "thanh toán", "PAYMENT")
        .Should().BeTrue();
    
    // Verify fake email service was called
    FakeEmailService.WasEmailSent(member.Email, "thanh toán")
        .Should().BeTrue();
}
```

---

## 🔧 **AVAILABLE SERVICES & UTILITIES**

### **Real Services (Available in InMemoryTestBase):**
- `BangLuongService` - Salary calculations
- `ThanhToanService` - Payment processing
- `BookingService` - Booking management
- `DiemDanhService` - Attendance tracking
- `BaoCaoService` - Reporting
- `DangKyService` - Registration management
- `GoiTapService` - Package management
- `LopHocService` - Class management
- `NguoiDungService` - User management

### **Fake Services (For External Dependencies):**
- `FakeEmailService` - Email operations
- `FakeNotificationService` - Notifications
- `FakeFileService` - File operations

### **Helper Methods:**
- `CreateTestUserAsync(role, status)` - Create test users
- `CreateTestRegistrationAsync(userId, packageId, status)` - Create registrations
- `CreateTestPaymentAsync(registrationId, amount, status)` - Create payments
- `ResetFakeServices()` - Clear fake service state

### **Test Data (Pre-created):**
- `TestUsers` - Sample users (Admin, Trainer, Members)
- `TestPackages` - Sample packages (Basic, Premium, VIP)
- `TestClasses` - Sample classes (Yoga, Cardio)

---

## ⚡ **QUICK MIGRATION CHECKLIST**

### **Before Starting:**
- [ ] Identify the service being tested
- [ ] List all mock objects used
- [ ] Understand the business logic being tested

### **During Migration:**
- [ ] Change base class to `InMemoryTestBase`
- [ ] Remove mock declarations
- [ ] Replace mock setups with real data creation
- [ ] Replace mock verifications with state assertions
- [ ] Use fake services for external dependencies
- [ ] Test the migration - all tests should pass

### **After Migration:**
- [ ] Verify test coverage is maintained
- [ ] Check test execution time (should be reasonable)
- [ ] Ensure tests are not flaky
- [ ] Update test documentation if needed

---

## 🎯 **BEST PRACTICES**

### **1. Data Isolation:**
```csharp
// Each test gets its own database instance
// No need to clean up between tests
```

### **2. Realistic Test Data:**
```csharp
// Create data that represents real business scenarios
var user = new NguoiDung 
{
    Ho = "Nguyen",
    Ten = "Van A",
    Email = "nguyenvana@gmail.com", // Realistic email
    SoDienThoai = "0123456789",     // Valid phone format
    NgayTao = DateTime.Now.AddMonths(-1) // Realistic creation date
};
```

### **3. Test Business Rules:**
```csharp
// Test actual business logic, not just data persistence
var commission = await BangLuongService.CalculateMonthlyCommissionAsync(trainerId, month, year);

// Verify business rule: commission should be 5% of package sales
var expectedCommission = packageSales * 0.05m;
commission.PackageCommission.Should().Be(expectedCommission);
```

### **4. Use Fake Services Appropriately:**
```csharp
// Reset fake services at the start of each test
ResetFakeServices();

// Configure fake services for specific test scenarios
FakeEmailService.ShouldThrowException = true; // Test error handling
```

---

## 🚀 **READY TO START?**

1. **Choose a simple test file** (look for files with few mock objects)
2. **Follow the step-by-step migration** above
3. **Run the tests** to ensure they pass
4. **Compare with example tests** in the `InMemory/` folder
5. **Ask for help** if you encounter issues

**Happy Testing!** 🎉
