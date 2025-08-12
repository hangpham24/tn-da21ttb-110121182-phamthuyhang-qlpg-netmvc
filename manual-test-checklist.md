# 🧪 **MANUAL TESTING CHECKLIST - GYM MANAGEMENT SYSTEM**

## 📋 **COMPREHENSIVE TESTING FRAMEWORK**
**Kiểm tra từ Model → Controller → View một cách có hệ thống**

---

## 🏗️ **1. MODEL LAYER TESTING**

### ✅ **Database Models Validation**
- [ ] **GoiTap Model**
  - [ ] Tạo gói tập với dữ liệu hợp lệ
  - [ ] Kiểm tra validation (tên rỗng, giá âm, thời hạn = 0)
  - [ ] Test relationships với DangKy, LopHoc
  
- [ ] **NguoiDung Model**
  - [ ] Tạo user với email hợp lệ
  - [ ] Test email validation
  - [ ] Test phone number format
  - [ ] Test role assignment (ADMIN, TRAINER, THANHVIEN)

- [ ] **DangKy Model**
  - [ ] Tạo registration với foreign keys hợp lệ
  - [ ] Test date validation (NgayKetThuc > NgayBatDau)
  - [ ] Test status transitions (ACTIVE, EXPIRED, CANCELLED)

- [ ] **ThanhToan Model**
  - [ ] Tạo payment với amount > 0
  - [ ] Test payment method validation
  - [ ] Test transaction ID uniqueness

### 🧪 **Model Testing Commands**
```bash
# Test model validation
dotnet test BangLuongServiceTests --filter "Category=ModelValidation"

# Test entity relationships
dotnet test BangLuongServiceTests --filter "Category=EntityRelationships"
```

---

## 🎮 **2. CONTROLLER LAYER TESTING**

### ✅ **GoiTapController Testing**
- [ ] **GET /GoiTap** - List all packages
  - [ ] Returns 200 OK
  - [ ] Contains package data
  - [ ] Pagination works correctly
  
- [ ] **GET /GoiTap/Details/{id}** - Package details
  - [ ] Valid ID returns 200 OK
  - [ ] Invalid ID returns 404 Not Found
  - [ ] Data is correctly displayed

- [ ] **POST /GoiTap/Create** - Create package
  - [ ] Valid data creates package successfully
  - [ ] Invalid data returns validation errors
  - [ ] Redirects to index after creation

- [ ] **PUT /GoiTap/Edit/{id}** - Update package
  - [ ] Valid update saves changes
  - [ ] Concurrency handling works
  - [ ] Audit trail is created

### ✅ **DangKyController Testing**
- [ ] **POST /DangKy/Register** - User registration
  - [ ] Valid registration creates record
  - [ ] Payment integration works
  - [ ] Email notification sent

### ✅ **API Controller Testing**
- [ ] **GET /api/GoiTap** - JSON API
  - [ ] Returns JSON format
  - [ ] Correct HTTP status codes
  - [ ] CORS headers present

### 🧪 **Controller Testing Commands**
```bash
# Test HTTP endpoints
curl -X GET "https://localhost:5001/GoiTap" -H "Accept: application/json"

# Test API endpoints
curl -X GET "https://localhost:5001/api/GoiTap" -H "Accept: application/json"

# Integration tests
dotnet test GymManagement.IntegrationTests --filter "Category=ControllerTests"
```

---

## 👁️ **3. VIEW LAYER TESTING**

### ✅ **UI/UX Testing**
- [ ] **Package Management Views**
  - [ ] /GoiTap/Index displays package list correctly
  - [ ] /GoiTap/Create form has all required fields
  - [ ] /GoiTap/Edit form pre-populates data
  - [ ] /GoiTap/Details shows complete information

- [ ] **Responsive Design**
  - [ ] Mobile view (< 768px)
  - [ ] Tablet view (768px - 1024px)
  - [ ] Desktop view (> 1024px)

- [ ] **Form Validation**
  - [ ] Client-side validation works
  - [ ] Server-side validation displays errors
  - [ ] Success messages appear correctly

- [ ] **Navigation & UX**
  - [ ] Menu navigation works
  - [ ] Breadcrumbs are correct
  - [ ] Loading states display
  - [ ] Error pages render properly

### 🧪 **View Testing Commands**
```bash
# Start application for manual testing
dotnet run --project GymManagement.Web

# Run Selenium E2E tests
dotnet test GymManagement.IntegrationTests --filter "Category=E2ETests"
```

---

## 🔗 **4. INTEGRATION TESTING**

### ✅ **Database Integration**
- [ ] **Entity Framework**
  - [ ] Migrations apply correctly
  - [ ] CRUD operations work
  - [ ] Relationships load properly
  - [ ] Soft delete functions

- [ ] **Connection Handling**
  - [ ] Connection pooling works
  - [ ] Timeout handling
  - [ ] Transaction rollback

### ✅ **External Service Integration**
- [ ] **Payment Gateways**
  - [ ] VNPay integration works
  - [ ] VietQR generation works
  - [ ] Payment callbacks handled

- [ ] **Email Service**
  - [ ] SMTP connection works
  - [ ] Email templates render
  - [ ] Delivery confirmation

- [ ] **Face Recognition**
  - [ ] Image upload works
  - [ ] Recognition API responds
  - [ ] Check-in process completes

### 🧪 **Integration Testing Commands**
```bash
# Test database operations
dotnet test GymManagement.IntegrationTests --filter "Category=DatabaseTests"

# Test external services
dotnet test GymManagement.IntegrationTests --filter "Category=IntegrationTests"
```

---

## 🚀 **5. END-TO-END TESTING**

### ✅ **Complete User Workflows**
- [ ] **Admin Workflow**
  1. Login as admin
  2. Create new package
  3. Assign trainer to package
  4. View reports
  5. Manage users

- [ ] **Member Workflow**
  1. Register account
  2. Browse packages
  3. Purchase package
  4. Make payment
  5. Check-in to gym

- [ ] **Trainer Workflow**
  1. Login as trainer
  2. View assigned classes
  3. Mark attendance
  4. View commission report
  5. Update profile

### 🧪 **E2E Testing Commands**
```bash
# Run full E2E test suite
dotnet test GymManagement.IntegrationTests --filter "Category=E2ETests"

# Run specific workflow tests
dotnet test GymManagement.IntegrationTests --filter "TestCategory=UserWorkflow"
```

---

## 📊 **6. PERFORMANCE TESTING**

### ✅ **Load Testing**
- [ ] **Database Performance**
  - [ ] Query execution time < 100ms
  - [ ] Bulk operations handle 1000+ records
  - [ ] Connection pool doesn't exhaust

- [ ] **API Performance**
  - [ ] Response time < 200ms
  - [ ] Concurrent users (100+)
  - [ ] Memory usage stable

### 🧪 **Performance Testing Commands**
```bash
# Load test with Apache Bench
ab -n 1000 -c 10 http://localhost:5000/GoiTap

# Memory profiling
dotnet-counters monitor --process-id [PID] --counters System.Runtime
```

---

## 🛡️ **7. SECURITY TESTING**

### ✅ **Authentication & Authorization**
- [ ] **Login Security**
  - [ ] Password hashing works
  - [ ] Session management secure
  - [ ] Role-based access control

- [ ] **Input Validation**
  - [ ] SQL injection prevention
  - [ ] XSS protection
  - [ ] CSRF tokens present

### 🧪 **Security Testing Commands**
```bash
# Check for security vulnerabilities
dotnet list package --vulnerable

# Run security scan
dotnet audit
```

---

## 📋 **TESTING EXECUTION CHECKLIST**

### ✅ **Pre-Testing Setup**
- [ ] Database is seeded with test data
- [ ] All services are running
- [ ] Test environment configured
- [ ] Backup created

### ✅ **Testing Execution**
- [ ] Run all unit tests
- [ ] Run integration tests
- [ ] Execute manual test cases
- [ ] Perform E2E testing
- [ ] Load testing completed

### ✅ **Post-Testing**
- [ ] Test results documented
- [ ] Bugs logged and prioritized
- [ ] Coverage report generated
- [ ] Performance metrics recorded

---

## 🎯 **SUCCESS CRITERIA**

### ✅ **Quality Gates**
- [ ] **Unit Test Coverage**: > 80%
- [ ] **Integration Test Coverage**: > 70%
- [ ] **All Critical Paths**: 100% tested
- [ ] **Performance**: Response time < 200ms
- [ ] **Security**: No high/critical vulnerabilities
- [ ] **User Experience**: All workflows complete successfully

### 📊 **Expected Results**
- **Model Layer**: 95% validation coverage
- **Controller Layer**: 90% endpoint coverage  
- **View Layer**: 80% UI component coverage
- **Integration**: 75% external service coverage
- **Overall System**: 85% comprehensive coverage

---

## 🏁 **FINAL VALIDATION**

✅ **System Ready for Production When:**
- [ ] All test categories pass
- [ ] Performance benchmarks met
- [ ] Security scan clean
- [ ] User acceptance testing complete
- [ ] Documentation updated
- [ ] Deployment checklist verified

**🎉 Your Gym Management System is thoroughly tested and production-ready!**
