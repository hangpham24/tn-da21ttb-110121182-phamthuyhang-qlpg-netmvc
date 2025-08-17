# Test Script cho Bug Fixes

## 🚨 **Vấn đề đã fix:**

### **1. AmbiguousMatchException - Duplicate CreateAccount methods**
- **Root Cause:** Có 2 methods `CreateAccount` với signatures khác nhau
- **Solution:** Xóa method cũ, chỉ giữ method mới với signature `CreateAccount(int userId)`

### **2. Create User không tạo TaiKhoan**
- **Root Cause:** `AuthService.CreateUserAsync()` cố tạo NguoiDung mới khi NguoiDung đã tồn tại
- **Solution:** Tạo method mới `CreateAccountForExistingUserAsync()` chỉ tạo TaiKhoan

## 🧪 **Test Cases cần thực hiện:**

### **Test Case 1: Fix AmbiguousMatchException**
1. Truy cập: http://localhost:5003/User?page=3
2. Click vào "..." trong cột thao tác của user chưa có tài khoản
3. Click "Tạo tài khoản"
4. **Expected:** Không còn lỗi AmbiguousMatchException
5. **Expected:** Modal hiển thị thông báo thành công với username/password

### **Test Case 2: Create User với TaiKhoan**
1. Truy cập: http://localhost:5003/User/Create
2. Điền thông tin user:
   - Họ: Nguyễn
   - Tên: Test User
   - Email: testuser@example.com
   - Số điện thoại: 0123456789
   - **Tên đăng nhập:** testuser123
   - **Mật khẩu:** password123
   - **Vai trò:** Member
3. Submit form
4. **Expected:** User được tạo thành công
5. **Expected:** Trong danh sách User, user mới hiển thị "testuser123" (có tài khoản)

### **Test Case 3: Verify Login với account mới tạo**
1. Sau khi tạo user thành công
2. Truy cập: http://localhost:5003/Auth/Login
3. Đăng nhập với:
   - Username: testuser123
   - Password: password123
4. **Expected:** Đăng nhập thành công

### **Test Case 4: Test Bulk Create Account**
1. Truy cập: http://localhost:5003/User
2. Click "Tạo tài khoản" trong header
3. **Expected:** Modal hiển thị danh sách users chưa có tài khoản
4. Click "Tạo tài khoản hàng loạt"
5. **Expected:** Tạo thành công, hiển thị kết quả với username/password

## 🔍 **Debug Information:**

### **Logs để monitor:**
```
SUCCESS: Created TaiKhoan for existing NguoiDung ID: {id}
Successfully created account for user {username} with role {role}
```

### **Database Queries để verify:**
```sql
-- Kiểm tra user mới tạo có TaiKhoan không
SELECT 
    nd.NguoiDungId,
    nd.Ho + ' ' + nd.Ten as HoTen,
    tk.TenDangNhap,
    tk.Email,
    tk.KichHoat
FROM NguoiDungs nd
LEFT JOIN TaiKhoans tk ON nd.NguoiDungId = tk.NguoiDungId
WHERE nd.NguoiDungId = (SELECT MAX(NguoiDungId) FROM NguoiDungs);

-- Kiểm tra roles được assign đúng không
SELECT 
    tk.TenDangNhap,
    vt.TenVaiTro
FROM TaiKhoans tk
JOIN TaiKhoanVaiTros tkvt ON tk.Id = tkvt.TaiKhoanId
JOIN VaiTros vt ON tkvt.VaiTroId = vt.VaiTroId
WHERE tk.TenDangNhap = 'testuser123';
```

## 🎯 **Expected Results:**

1. **No More AmbiguousMatchException:** Dropdown actions hoạt động bình thường
2. **Create User Success:** User mới có cả NguoiDung và TaiKhoan records
3. **Login Success:** Có thể đăng nhập với username/password mới tạo
4. **UI Display:** Username hiển thị đúng trong danh sách User
5. **Role Assignment:** User được assign role đúng theo loại người dùng

## 🚨 **Nếu vẫn có vấn đề:**

### **Issue 1: Vẫn lỗi AmbiguousMatchException**
- Check: Có thể còn duplicate methods khác
- Solution: Search toàn bộ controller cho duplicate method names

### **Issue 2: TaiKhoan vẫn không được tạo**
- Check: Logs trong `CreateAccountForExistingUserAsync`
- Check: Username/email có bị duplicate không
- Check: NguoiDungId có tồn tại không

### **Issue 3: Không thể đăng nhập**
- Check: Password hash có đúng không
- Check: User có bị vô hiệu hóa không
- Check: Role có được assign đúng không

## 💡 **Additional Notes:**

- Method `CreateAccountForExistingUserAsync` chỉ tạo TaiKhoan, không tạo NguoiDung
- Method `CreateUserAsync` tạo cả NguoiDung và TaiKhoan (dùng cho registration)
- Debug logs đã được thêm để dễ troubleshoot
- All existing functionality should remain unchanged
