# 🎓 **DEMO SCRIPT - TÍNH NĂNG KHÁCH VÃNG LAI**
## Đồ án tốt nghiệp CNTT - Hệ thống quản lý phòng gym

---

## 📋 **CHUẨN BỊ DEMO**

### **🔧 Bước 1: Khởi động hệ thống**
```bash
# Mở terminal tại thư mục dự án
cd d:\DATN\HANG_REMOTE\qlpg\GymManagement.Web

# Chạy ứng dụng
dotnet run
```

### **🗄️ Bước 2: Chuẩn bị dữ liệu test**
```sql
-- Chạy script tạo dữ liệu test
-- File: Database/TestWalkInFeature.sql
-- Hoặc chạy: Database/SeedWalkInPackages.sql
```

### **🌐 Bước 3: Truy cập hệ thống**
- URL: `https://localhost:7139`
- Đăng nhập: Admin/Trainer account
- Điều hướng: Reception → Station

---

## 🎬 **KỊCH BẢN DEMO CHI TIẾT**

### **🎯 Phần 1: Giới thiệu tổng quan (2 phút)**

**"Chào thầy/cô và các bạn. Hôm nay em xin demo tính năng **Quản lý khách vãng lai** trong hệ thống quản lý phòng gym."**

#### **Vấn đề cần giải quyết:**
- Khách hàng muốn tập thử không cần đăng ký thành viên
- Cần quy trình nhanh gọn tại quầy lễ tân
- Quản lý thanh toán và điểm danh riêng biệt
- Báo cáo doanh thu phân tách theo nguồn khách

#### **Giải pháp đề xuất:**
- Form đăng ký nhanh (chỉ cần tên + SĐT)
- Thanh toán linh hoạt (tiền mặt/chuyển khoản)
- Check-in/check-out tự động
- Báo cáo doanh thu riêng biệt

---

### **🖥️ Phần 2: Demo giao diện (3 phút)**

#### **2.1 Truy cập Reception Station**
```
1. Mở browser → https://localhost:7139
2. Đăng nhập với tài khoản Admin
3. Menu → Reception → Station
4. Hiển thị 2 tabs:
   - 👁️ Nhận diện khuôn mặt (cho thành viên)
   - 🚶 Khách vãng lai (tính năng mới)
```

**Thuyết trình:**
> "Đây là giao diện Reception Station với 2 chức năng chính. Tab đầu tiên dành cho thành viên chính thức với nhận diện khuôn mặt. Tab thứ hai là tính năng mới cho khách vãng lai."

#### **2.2 Giao diện Khách vãng lai**
```
Click tab "🚶 Khách vãng lai"
→ Hiển thị 2 panel:
   - Trái: Form đăng ký nhanh
   - Phải: Danh sách khách đang tập hôm nay
```

**Thuyết trình:**
> "Giao diện được thiết kế đơn giản với 2 khu vực chính. Bên trái là form đăng ký nhanh, bên phải là danh sách theo dõi khách đang tập."

---

### **🎪 Phần 3: Demo tính năng thực tế (8 phút)**

#### **3.1 Kịch bản 1: Đăng ký + Thanh toán tiền mặt (3 phút)**

**Setup:**
```
Form đăng ký nhanh:
- Họ tên: "Nguyễn Văn Demo"
- SĐT: "0123456789"
- Gói vé: "Vé ngày - 50,000 VNĐ"
- Phương thức: "💵 Tiền mặt"
```

**Thao tác demo:**
1. **Nhập thông tin:**
   ```
   Họ tên: Nguyễn Văn Demo
   SĐT: 0123456789
   ```
   > "Chỉ cần 2 thông tin cơ bản, không phức tạp như đăng ký thành viên."

2. **Chọn gói vé:**
   ```
   Dropdown hiển thị:
   - Vé ngày - 50,000 VNĐ
   - Vé 3 giờ - 30,000 VNĐ  
   - Vé buổi sáng - 35,000 VNĐ
   - Vé buổi chiều - 40,000 VNĐ
   ```
   > "Hệ thống có sẵn các gói vé linh hoạt cho khách vãng lai."

3. **Chọn thanh toán:**
   ```
   Radio buttons:
   ○ 💵 Tiền mặt (chọn)
   ○ 🏦 Chuyển khoản
   ```

4. **Xử lý thanh toán:**
   ```
   Click "Đăng ký và thanh toán"
   → Loading spinner
   → Thông báo: "Thanh toán và check-in thành công!"
   ```
   > "Với tiền mặt, hệ thống tự động xử lý và check-in luôn cho khách."

5. **Kiểm tra kết quả:**
   ```
   Panel bên phải cập nhật:
   ┌─────────────────┬──────────┬─────────┬──────────┐
   │ Nguyễn Văn Demo │ 14:30    │ Vé ngày │[Check-out]│
   │ 📞 0123456789   │ 🟢 Đang tập      │          │
   └─────────────────┴──────────┴─────────┴──────────┘
   ```
   > "Khách đã được check-in và hiển thị trong danh sách đang tập."

#### **3.2 Kịch bản 2: Thanh toán chuyển khoản (2 phút)**

**Setup:**
```
Khách mới:
- Họ tên: "Trần Thị B"
- SĐT: "0987654321"
- Gói vé: "Vé 3 giờ - 30,000 VNĐ"
- Phương thức: "🏦 Chuyển khoản"
```

**Thao tác demo:**
1. **Nhập thông tin và chọn BANK**
2. **Hiển thị QR code:**
   ```
   → Popup hiển thị:
     - Mã QR VietQR
     - Thông tin chuyển khoản:
       * Ngân hàng: Vietcombank
       * STK: 1234567890
       * Số tiền: 30,000 VNĐ
       * Nội dung: WALKIN_20240108_002
   ```
   > "Hệ thống tự động tạo QR code với thông tin chuyển khoản chính xác."

3. **Mô phỏng khách chuyển khoản:**
   ```
   (Dùng điện thoại quét QR - không chuyển tiền thật)
   → Click "Xác nhận đã nhận tiền"
   → Thông báo: "Xác nhận thanh toán và check-in thành công!"
   ```

#### **3.3 Kịch bản 3: Check-out khách (1 phút)**

**Thao tác:**
```
1. Tìm khách trong danh sách "Đang tập"
2. Click nút "Check-out" 
3. Popup xác nhận → Click "Xác nhận"
4. Thông báo: "Check-out thành công lúc 16:45"
5. Trạng thái chuyển: 🟢 Đang tập → ⚫ Đã xong
6. Hiển thị thời gian tập: "Thời gian tập: 2h 15m"
```

**Thuyết trình:**
> "Nhân viên có thể check-out khách khi họ ra về. Hệ thống tự động tính thời gian tập thực tế."

#### **3.4 Demo nhận diện khuôn mặt (2 phút)**

**Chuyển sang tab "👁️ Nhận diện khuôn mặt":**
```
1. Ngồi trước webcam laptop
2. Hệ thống tự động nhận diện
3. Hiển thị thông tin thành viên:
   - Tên: [Tên đã đăng ký]
   - Gói tập hiện tại
   - Lịch sử check-in
   - Nút check-in/check-out
```

**Thuyết trình:**
> "Đây là tính năng nhận diện khuôn mặt cho thành viên chính thức. Khác với khách vãng lai, thành viên có thể check-in tự động bằng AI."

---

### **📊 Phần 4: Demo báo cáo (2 phút)**

#### **4.1 Truy cập báo cáo doanh thu**
```
1. Menu → Báo cáo → Doanh thu
2. URL: /BaoCao/Revenue
```

#### **4.2 Demo filter nguồn khách**
```
1. Bộ lọc "Nguồn khách":
   - 🏢 Tất cả
   - 👥 Thành viên  
   - 🚶 Khách vãng lai (chọn)

2. Chọn khoảng thời gian: Hôm nay

3. Click "Áp dụng"
```

#### **4.3 Kết quả báo cáo**
```
📊 Biểu đồ doanh thu:
- Chỉ hiển thị doanh thu từ khách vãng lai
- Phân tách theo phương thức thanh toán

📈 Thống kê:
- Tổng doanh thu: 350,000 VNĐ
- Số lượt khách: 7 người
- Tiền mặt: 71% | Chuyển khoản: 29%
```

**Thuyết trình:**
> "Hệ thống tự động phân tách doanh thu theo nguồn khách, giúp quản lý theo dõi hiệu quả kinh doanh từng phân khúc."

---

## 🎯 **PHẦN 5: Tổng kết và Q&A (3 phút)**

### **💡 Điểm nổi bật của hệ thống:**

1. **Quy trình đơn giản:**
   - Chỉ 3 bước: Đăng ký → Thanh toán → Vào tập
   - Không cần tạo tài khoản phức tạp

2. **Thanh toán linh hoạt:**
   - Hỗ trợ tiền mặt và QR code
   - Tự động xử lý và check-in

3. **Quản lý thời gian:**
   - Theo dõi check-in/check-out tự động
   - Tính thời gian tập thực tế

4. **Báo cáo chi tiết:**
   - Phân tách doanh thu theo nguồn
   - Thống kê theo phương thức thanh toán

5. **Tích hợp AI:**
   - Nhận diện khuôn mặt cho thành viên
   - So sánh với quy trình khách vãng lai

### **🚀 Khả năng mở rộng:**
- Tích hợp SMS thông báo
- Ứng dụng mobile cho khách hàng
- Phân tích dữ liệu nâng cao
- Tích hợp với hệ thống POS

### **📚 Kiến thức áp dụng:**
- ASP.NET Core 8 MVC
- Entity Framework Core
- Tailwind CSS responsive design
- Face-API.js cho AI
- VietQR API integration
- SQL Server database design

---

## ❓ **CÂU HỎI THƯỜNG GẶP**

### **Q: Tại sao không dùng nhận diện khuôn mặt cho khách vãng lai?**
A: Khách vãng lai chỉ đến 1 lần nên không cần đăng ký khuôn mặt. Quy trình thủ công đơn giản hơn và phù hợp với nhu cầu.

### **Q: Làm sao phân biệt được doanh thu từ nguồn khách khác nhau?**
A: Hệ thống lưu LoaiNguoiDung trong database và có filter riêng trong báo cáo để phân tách doanh thu.

### **Q: Nếu khách vãng lai muốn trở thành thành viên thì sao?**
A: Có thể chuyển đổi từ VANGLAI sang THANHVIEN và tạo tài khoản đầy đủ với các tính năng nâng cao.

### **Q: Hệ thống có xử lý được nhiều khách cùng lúc không?**
A: Có, mỗi giao dịch được xử lý độc lập và có loading state để tránh conflict.

---

## 🎉 **KẾT THÚC DEMO**

**"Cảm ơn thầy/cô và các bạn đã theo dõi. Em xin kết thúc phần demo tính năng Quản lý khách vãng lai. Mọi người có câu hỏi gì không ạ?"**

---

### **📝 Ghi chú cho người demo:**
- Chuẩn bị sẵn dữ liệu test
- Kiểm tra kết nối internet cho VietQR
- Test webcam trước khi demo
- Chuẩn bị điện thoại để demo QR code
- Backup plan nếu có lỗi kỹ thuật
