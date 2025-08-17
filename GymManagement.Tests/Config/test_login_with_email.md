# Test Script cho Login với Username hoặc Email

## 🎯 **Tính năng mới đã implement:**

### **Enhanced Authentication Logic:**
- `AuthService.AuthenticateAsync()` giờ hỗ trợ cả username và email
- Tự động detect input là email hay username
- Fallback mechanism: thử username trước, nếu không có thì thử email

### **UI Improvements:**
- Label thay đổi từ "Tên đăng nhập" thành "Tên đăng nhập hoặc Email"
- Placeholder text cập nhật
- Thêm helper text giải thích

## 🧪 **Test Cases cần thực hiện:**

### **Test Case 1: Đăng nhập bằng Username**
1. Truy cập: http://localhost:5003/Auth/Login
2. Nhập username (ví dụ: `phamthuyhang`)
3. Nhập password
4. Verify đăng nhập thành công

### **Test Case 2: Đăng nhập bằng Email**
1. Truy cập: http://localhost:5003/Auth/Login
2. Nhập email (ví dụ: `hang@example.com`)
3. Nhập password
4. Verify đăng nhập thành công

### **Test Case 3: Test với Reset Password**
1. Reset password cho user từ Admin panel
2. Copy username từ modal
3. Copy password từ modal
4. Test đăng nhập bằng username
5. Test đăng nhập bằng email (nếu user có email)

### **Test Case 4: Error Handling**
1. Test với username/email không tồn tại
2. Test với password sai
3. Test với user bị vô hiệu hóa
4. Verify error messages hiển thị đúng

## 🔍 **Debug Steps nếu vẫn có vấn đề:**

### **Bước 1: Kiểm tra Database**
```sql
-- Kiểm tra user có TaiKhoan không
SELECT 
    nd.NguoiDungId,
    nd.Ho + ' ' + nd.Ten as HoTen,
    tk.TenDangNhap,
    tk.Email,
    tk.KichHoat,
    tk.Salt,
    tk.MatKhauHash
FROM NguoiDungs nd
LEFT JOIN TaiKhoans tk ON nd.NguoiDungId = tk.NguoiDungId
WHERE nd.NguoiDungId = 16;
```

### **Bước 2: Test Password Manually**
```csharp
// Trong controller hoặc service, test password verification
var user = await _authService.GetUserByUsernameAsync("username_here");
if (user != null)
{
    var isValid = _passwordService.VerifyPassword("H4rFAEnv", user.Salt, user.MatKhauHash);
    Console.WriteLine($"Password verification result: {isValid}");
}
```

### **Bước 3: Check Authentication Flow**
- Kiểm tra logs khi đăng nhập
- Verify AuthenticateAsync được gọi đúng
- Check user được tìm thấy không
- Verify password verification result

## 🚨 **Potential Issues và Solutions:**

### **Issue 1: User không có TaiKhoan record**
- **Check:** Query database để verify TaiKhoan tồn tại
- **Solution:** Tạo TaiKhoan nếu chưa có

### **Issue 2: Password hash không match**
- **Check:** Verify salt và hash trong database
- **Solution:** Re-hash password với salt đúng

### **Issue 3: User bị vô hiệu hóa**
- **Check:** Kiểm tra field `KichHoat` trong TaiKhoans table
- **Solution:** Set `KichHoat = true`

### **Issue 4: Email vs Username confusion**
- **Check:** Verify input được xử lý đúng
- **Solution:** Enhanced logic đã handle cả hai

## 📝 **Expected Results:**

1. **Login Page:** Hiển thị "Tên đăng nhập hoặc Email" với helper text
2. **Username Login:** Hoạt động như trước
3. **Email Login:** Hoạt động mới, tìm user bằng email
4. **Reset Password:** Username và password hiển thị đúng trong modal
5. **Error Handling:** Thông báo lỗi email rõ ràng

## 🎯 **Recommended Test Sequence:**

1. **Test UI first:** Verify login page hiển thị đúng
2. **Test existing functionality:** Đăng nhập bằng username
3. **Test new functionality:** Đăng nhập bằng email
4. **Test reset password:** Với user có email hợp lệ
5. **Test error cases:** Email không hợp lệ, user không tồn tại

## 💡 **Tips:**

- Sử dụng browser developer tools để check network requests
- Monitor console logs cho debug information
- Test với multiple users để verify consistency
- Check database state trước và sau mỗi operation
