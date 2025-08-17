# Test Script cho Reset Password Functionality

## 🔍 **Các bước test cần thực hiện:**

### **1. Test UI Changes (Yêu cầu 1)**
- [ ] Truy cập http://localhost:5003/User
- [ ] Kiểm tra không còn cột "Username" riêng biệt
- [ ] Verify username hiển thị ngay dưới NguoiDungId trong cột "Người dùng"
- [ ] Kiểm tra responsive design trên mobile/tablet

### **2. Test Reset Password (Vấn đề nghiêm trọng)**
- [ ] Truy cập http://localhost:5003/User/Edit/{userId}
- [ ] Click "Đặt lại mật khẩu"
- [ ] Kiểm tra modal hiển thị đúng username và password
- [ ] Copy username và password từ modal
- [ ] Đăng xuất và thử đăng nhập với thông tin mới tại http://localhost:5003/Auth/Login
- [ ] Verify đăng nhập thành công

### **3. Test Error Handling (Yêu cầu 2)**
- [ ] Test với user có email không hợp lệ
- [ ] Verify hiển thị cảnh báo khi không gửi được email
- [ ] Test với user không có email
- [ ] Kiểm tra thông báo lỗi hiển thị đúng

### **4. Test Dropdown Menu**
- [ ] Click vào "..." trong cột thao tác
- [ ] Test từng option trong dropdown
- [ ] Verify dropdown đóng khi click outside

### **5. Test Create Account Functions**
- [ ] Test tạo tài khoản đơn lẻ từ dropdown
- [ ] Test bulk create account từ header button
- [ ] Verify username generation logic

## 🐛 **Debug Steps cho Reset Password Issue:**

### **Bước 1: Kiểm tra Database**
```sql
-- Kiểm tra TaiKhoan của user bị lỗi
SELECT * FROM TaiKhoans WHERE NguoiDungId = 16;

-- Kiểm tra username
SELECT TenDangNhap, Email, KichHoat FROM TaiKhoans WHERE NguoiDungId = 16;
```

### **Bước 2: Kiểm tra Logs**
- Xem logs trong console khi reset password
- Tìm debug messages từ AuthService.ResetPasswordAsync
- Kiểm tra có exception nào không

### **Bước 3: Manual Test Password Hashing**
```csharp
// Test trong controller hoặc service
var testPassword = "H4rFAEnv";
var testSalt = "your_salt_from_db";
var testHash = _passwordService.HashPassword(testPassword, testSalt);
var verifyResult = _passwordService.VerifyPassword(testPassword, testSalt, testHash);
```

## 🔧 **Potential Issues và Solutions:**

### **Issue 1: User chưa có TaiKhoan**
- Solution: Đã thêm check `!user.HasAccount` trong controller
- Hiển thị message rõ ràng

### **Issue 2: Salt/Hash không match**
- Solution: Đã thêm debug logging trong AuthService
- Verify password ngay sau khi save

### **Issue 3: Username không đúng**
- Solution: Hiển thị username trong response
- User có thể copy chính xác username

### **Issue 4: Email errors**
- Solution: Đã thêm error handling và warning display
- Admin sẽ biết khi email không gửi được

## 📝 **Expected Results:**

1. **UI:** Username hiển thị dưới NguoiDungId, không có cột riêng
2. **Reset Password:** Hoạt động đúng, có thể đăng nhập với password mới
3. **Error Handling:** Hiển thị cảnh báo khi email thất bại
4. **Dropdown:** Hoạt động mượt mà, đóng khi click outside
5. **Create Account:** Tạo được tài khoản đơn lẻ và hàng loạt

## 🚨 **Nếu vẫn có vấn đề:**

1. Kiểm tra database connection
2. Verify user có TaiKhoan record
3. Check password hashing algorithm
4. Verify username chính xác
5. Test với user khác
