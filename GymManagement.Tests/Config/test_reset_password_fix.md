# Test Script cho Reset Password Fix

## 🚨 **Vấn đề đã được fix:**

### **Root Cause:**
- `NguoiDungService.GetByIdAsync()` sử dụng `GetByIdAsync()` thông thường (không include TaiKhoan)
- Dẫn đến `HasAccount` luôn = false vì `nguoiDung.TaiKhoan` = null
- Reset Password check `HasAccount` và fail

### **Solution:**
- Thay đổi `GetByIdAsync()` để sử dụng `GetWithTaiKhoanAsync()` (có include TaiKhoan)
- Sử dụng `MapToDtoWithTaiKhoan()` thay vì `MapToDto()`
- Đảm bảo `HasAccount` được set đúng: `nguoiDung.TaiKhoan != null`

## 🧪 **Test Cases:**

### **Test Case 1: Reset Password cho User có TaiKhoan**
1. **Truy cập:** http://localhost:5003/User/Edit/47
2. **Verify:** User ID 47 có TaiKhoan trong database:
   ```sql
   SELECT * FROM TaiKhoans WHERE NguoiDungId = 47;
   ```
3. **Click:** "Đặt lại mật khẩu"
4. **Expected:** 
   - Không còn lỗi "User does not have an account"
   - Modal hiển thị với username và password mới
   - Có thể copy username/password

### **Test Case 2: Verify HasAccount Logic**
1. **Check database:** User 47 có TaiKhoan record
2. **Check UI:** Trong danh sách User, user 47 hiển thị username (không phải "Chưa có TK")
3. **Expected:** `HasAccount = true` được detect đúng

### **Test Case 3: Login với Password mới**
1. **Sau khi reset password thành công**
2. **Copy username và password từ modal**
3. **Truy cập:** http://localhost:5003/Auth/Login
4. **Đăng nhập với credentials mới**
5. **Expected:** Đăng nhập thành công

### **Test Case 4: Test với User chưa có TaiKhoan**
1. **Tìm user chưa có TaiKhoan** (hiển thị "Chưa có TK")
2. **Truy cập Edit page của user đó**
3. **Click "Đặt lại mật khẩu"**
4. **Expected:** Hiển thị message "Người dùng chưa có tài khoản"

## 🔍 **Debug Information:**

### **Database Verification:**
```sql
-- Check user 47 có TaiKhoan không
SELECT 
    nd.NguoiDungId,
    nd.Ho + ' ' + nd.Ten as HoTen,
    tk.TenDangNhap,
    tk.Email,
    tk.KichHoat,
    CASE WHEN tk.NguoiDungId IS NOT NULL THEN 'TRUE' ELSE 'FALSE' END as HasAccount
FROM NguoiDungs nd
LEFT JOIN TaiKhoans tk ON nd.NguoiDungId = tk.NguoiDungId
WHERE nd.NguoiDungId = 47;
```

### **Expected Logs:**
```
=== ResetPassword START for UserId: 47 ===
Attempting to reset password for UserId: 47, Username: thuyhang
DEBUG: ResetPasswordAsync called for NguoiDungId: 47
SUCCESS: Password reset for TaiKhoan ID: {id}
```

### **No More Error Logs:**
```
❌ User ID: 47 does not have an account (SHOULD NOT APPEAR)
```

## 🎯 **Expected Results:**

1. **Reset Password Success:** Modal hiển thị với username và password mới
2. **HasAccount Detection:** User 47 hiển thị username trong danh sách (không phải "Chưa có TK")
3. **Login Success:** Có thể đăng nhập với password mới
4. **No Authorization Errors:** Không còn redirect về login page

## 🚨 **Potential Issues:**

### **Issue 1: Vẫn lỗi "does not have an account"**
- **Check:** Database có TaiKhoan record với NguoiDungId = 47 không
- **Check:** GetWithTaiKhoanAsync có include TaiKhoan đúng không

### **Issue 2: HasAccount vẫn false**
- **Check:** MapToDtoWithTaiKhoan có được gọi không
- **Check:** TaiKhoan navigation property có được load không

### **Issue 3: Không thể đăng nhập với password mới**
- **Check:** Password hash có được update đúng không
- **Check:** Salt có được regenerate không

## 💡 **Technical Details:**

### **Before Fix:**
```csharp
// NguoiDungService.GetByIdAsync()
var nguoiDung = await _unitOfWork.NguoiDungs.GetByIdAsync(id); // No include
return nguoiDung != null ? MapToDto(nguoiDung) : null; // No TaiKhoan info
```

### **After Fix:**
```csharp
// NguoiDungService.GetByIdAsync()
var nguoiDung = await _unitOfWork.NguoiDungs.GetWithTaiKhoanAsync(id); // Include TaiKhoan
return nguoiDung != null ? MapToDtoWithTaiKhoan(nguoiDung) : null; // With TaiKhoan info
```

### **Key Changes:**
1. **Repository Method:** `GetByIdAsync()` → `GetWithTaiKhoanAsync()`
2. **Mapping Method:** `MapToDto()` → `MapToDtoWithTaiKhoan()`
3. **Include TaiKhoan:** Navigation property được load
4. **HasAccount Logic:** `nguoiDung.TaiKhoan != null` hoạt động đúng

## 🔧 **Files Modified:**

1. **Services/NguoiDungService.cs:**
   - Updated `GetByIdAsync()` method
   - Use `GetWithTaiKhoanAsync()` instead of `GetByIdAsync()`
   - Use `MapToDtoWithTaiKhoan()` instead of `MapToDto()`

## 📝 **Additional Notes:**

- Fix này ảnh hưởng đến tất cả nơi gọi `NguoiDungService.GetByIdAsync()`
- Đảm bảo performance không bị ảnh hưởng vì include TaiKhoan
- Repository đã có sẵn method `GetWithTaiKhoanAsync()` nên không cần thay đổi
- Mapping method `MapToDtoWithTaiKhoan()` đã có sẵn và hoạt động đúng
