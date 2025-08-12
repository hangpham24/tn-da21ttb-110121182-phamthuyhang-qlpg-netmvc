# 🏋️ Gym Management System

Hệ thống quản lý phòng gym hiện đại và chuyên nghiệp được xây dựng bằng ASP.NET Core 8.0, Entity Framework Core, và Tailwind CSS.

## ✨ Tính năng chính

### 👥 Quản lý người dùng
- Đăng ký/đăng nhập với ASP.NET Core Identity
- Phân quyền theo vai trò (Admin, Trainer, Member)
- Quản lý thông tin cá nhân và avatar
- Hệ thống thông báo tích hợp

### 📦 Quản lý gói tập
- Tạo và quản lý các gói tập khác nhau
- Thiết lập giá và thời hạn linh hoạt
- Hệ thống khuyến mãi và giảm giá

### 🏃‍♂️ Quản lý lớp học
- Tạo lịch học tự động
- Quản lý sức chứa lớp học
- Đăng ký và hủy lớp học
- Theo dõi tình trạng lớp học

### ✅ Điểm danh thông minh
- Điểm danh thủ công
- Nhận diện khuôn mặt (Face Recognition)
- Theo dõi lịch sử điểm danh
- Báo cáo thống kê điểm danh

### 💳 Thanh toán đa dạng
- Thanh toán tiền mặt
- Tích hợp VNPay
- Quản lý hóa đơn và biên lai
- Hệ thống hoàn tiền

### 📊 Báo cáo và thống kê
- Dashboard tổng quan
- Báo cáo doanh thu
- Thống kê thành viên
- Phân tích hiệu suất

### 💰 Quản lý lương
- Tính lương tự động cho HLV
- Hệ thống hoa hồng
- Báo cáo chi phí nhân sự

## 🛠️ Công nghệ sử dụng

- **Backend**: ASP.NET Core 8.0
- **Database**: SQL Server với Entity Framework Core
- **Authentication**: ASP.NET Core Identity
- **Frontend**: Razor Pages + Tailwind CSS + Flowbite
- **Logging**: Serilog
- **Email**: MailKit
- **Payment**: VNPay Integration
- **Charts**: Chart.js

## 📋 Yêu cầu hệ thống

- .NET 8.0 SDK
- SQL Server (LocalDB hoặc SQL Server Express)
- Visual Studio 2022 hoặc VS Code
- Node.js (cho Tailwind CSS - tùy chọn)

## 🚀 Cài đặt và chạy

### 1. Clone repository
```bash
git clone https://github.com/daihoangphuc/qlpg.git
cd qlpg
```

### 2. Cấu hình database
Cập nhật connection string trong `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "GymDb": "Server=(localdb)\\mssqllocaldb;Database=GymManagementDb;Trusted_Connection=true;MultipleActiveResultSets=true"
  }
}
```

### 3. Cài đặt packages
```bash
dotnet restore
```

### 4. Tạo database
```bash
dotnet ef database update
```

### 5. Chạy ứng dụng
```bash
dotnet run
```

Ứng dụng sẽ chạy tại: `https://localhost:7000`

## 👤 Tài khoản mặc định

Sau khi khởi tạo database, hệ thống sẽ tạo các tài khoản mặc định:

| Tài khoản | Mật khẩu | Vai trò |
|-----------|----------|---------|
| admin | Admin@123 | Admin |
| trainer1 | Trainer@123 | Trainer |
| trainer2 | Trainer@123 | Trainer |
| member1 | Member@123 | Member |

## 📁 Cấu trúc dự án

```
GymManagement.Web/
├── Controllers/          # MVC Controllers
├── Data/
│   ├── Models/          # Entity Models
│   ├── Repositories/    # Repository Pattern
│   └── GymDbContext.cs  # Database Context
├── Services/            # Business Logic Services
├── Views/               # Razor Views
├── wwwroot/            # Static Files
├── Models/
│   ├── DTOs/           # Data Transfer Objects
│   └── ViewModels/     # View Models
└── Program.cs          # Application Entry Point
```

## 🔧 Cấu hình

### Email Settings
Cập nhật cấu hình email trong `appsettings.json`:
```json
{
  "EmailSettings": {
    "SmtpServer": "smtp.gmail.com",
    "SmtpPort": 587,
    "SmtpUsername": "your-email@gmail.com",
    "SmtpPassword": "your-app-password",
    "SenderEmail": "your-email@gmail.com",
    "SenderName": "Gym Management System"
  }
}
```

### VNPay Settings
Cấu hình VNPay cho thanh toán online:
```json
{
  "VnPay": {
    "TmnCode": "YOUR_TMN_CODE",
    "HashSecret": "YOUR_HASH_SECRET",
    "BaseUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html"
  }
}
```

## 📱 Tính năng nổi bật

### 🎨 Giao diện hiện đại
- Responsive design với Tailwind CSS
- Dark/Light mode
- Mobile-friendly
- Accessibility support

### 🔐 Bảo mật
- ASP.NET Core Identity
- Role-based authorization
- HTTPS enforcement
- Input validation

### 📈 Hiệu suất
- Entity Framework Core optimization
- Memory caching
- Lazy loading
- Connection pooling

### 🔍 Tìm kiếm và lọc
- Real-time search
- Advanced filtering
- Pagination
- Sorting

## 📖 Technical Documentation

### Architecture Overview

#### Layered Architecture
Dự án sử dụng kiến trúc phân lớp (Layered Architecture) với các thành phần chính:

1. **Presentation Layer (Views + Controllers)**
   - Razor Views cho UI
   - Controllers xử lý HTTP requests
   - ViewModels và DTOs để truyền dữ liệu

2. **Business Layer (Services)**
   - Business logic được đóng gói trong các Services
   - Dependency Injection pattern
   - Separation of Concerns

3. **Data Access Layer (Repositories + DbContext)**
   - Repository Pattern cho data access
   - Unit of Work pattern
   - Entity Framework Core ORM

#### Design Patterns
- **Repository Pattern**: Abstraction cho data access
- **Unit of Work**: Transaction management
- **Dependency Injection**: Loose coupling giữa các components
- **DTO Pattern**: Data transfer giữa các layers

### Database Schema

#### Core Tables
- `NguoiDung`: Thông tin người dùng
- `TaiKhoan`: Authentication accounts
- `VaiTro`: Roles (Admin, Trainer, Member)
- `GoiTap`: Gym packages
- `LopHoc`: Classes
- `DangKy`: Registrations
- `ThanhToan`: Payments
- `DiemDanh`: Attendance

### API Endpoints

#### Authentication
- `POST /Auth/Login` - Đăng nhập
- `POST /Auth/Register` - Đăng ký
- `POST /Auth/Logout` - Đăng xuất

#### User Management
- `GET /User` - Danh sách người dùng
- `POST /User/Create` - Tạo người dùng
- `PUT /User/Edit/{id}` - Cập nhật người dùng
- `DELETE /User/Delete/{id}` - Xóa người dùng

#### Package Management
- `GET /GoiTap` - Danh sách gói tập
- `POST /GoiTap/Create` - Tạo gói tập
- `PUT /GoiTap/Edit/{id}` - Cập nhật gói tập

### Security Implementation

#### Authentication & Authorization
```csharp
// Cookie-based authentication
services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
    });

// Role-based authorization
[Authorize(Roles = "Admin")]
public IActionResult AdminDashboard() { }
```

#### Password Security
- BCrypt hashing với salt
- Minimum password requirements
- Password reset functionality

### Performance Optimizations

1. **Database Queries**
   - Include() để prevent N+1 queries
   - AsNoTracking() cho read-only queries
   - Pagination với Skip() và Take()

2. **Caching**
   - Memory caching cho static data
   - Response caching cho public pages

3. **Client-side**
   - Bundling và minification
   - CDN cho static resources
   - Lazy loading images

## 📘 User Manual

### Cho Quản trị viên (Admin)

#### 1. Đăng nhập hệ thống
1. Truy cập trang đăng nhập
2. Nhập tài khoản: `admin` và mật khẩu: `Admin@123`
3. Click "Đăng nhập"

#### 2. Quản lý người dùng
1. Vào menu "👥 Quản lý người dùng"
2. Chọn loại người dùng cần quản lý (Thành viên/HLV/Nhân viên)
3. Thực hiện các thao tác:
   - **Thêm mới**: Click "Thêm người dùng"
   - **Sửa**: Click icon ✏️ trên dòng cần sửa
   - **Xóa**: Click icon 🗑️ và xác nhận

#### 3. Quản lý gói tập
1. Vào menu "💳 Quản lý gói tập"
2. Click "Thêm gói tập" để tạo mới
3. Điền thông tin:
   - Tên gói
   - Thời hạn (tháng)
   - Giá tiền
   - Mô tả

#### 4. Quản lý lớp học
1. Vào menu "🎓 Quản lý lớp học"
2. Click "Tạo lớp học"
3. Chọn HLV phụ trách
4. Thiết lập lịch học và sức chứa

#### 5. Xem báo cáo
1. Vào menu "📊 Báo cáo"
2. Chọn loại báo cáo:
   - Doanh thu
   - Thành viên
   - Lớp học
3. Chọn khoảng thời gian
4. Click "Xuất báo cáo" để tải Excel

### Cho Thành viên (Member)

#### 1. Đăng ký tài khoản
1. Click "Đăng ký ngay" trên trang chủ
2. Điền thông tin cá nhân
3. Tạo tài khoản với email và mật khẩu
4. Xác nhận đăng ký

#### 2. Đăng ký gói tập
1. Vào "Gói tập" từ menu
2. Chọn gói phù hợp
3. Click "Đăng ký ngay"
4. Chọn phương thức thanh toán:
   - Tiền mặt: Đến quầy thanh toán
   - VNPay: Thanh toán online

#### 3. Đăng ký lớp học
1. Vào "Lớp học" từ menu
2. Xem lịch và chọn lớp phù hợp
3. Click "Đăng ký"
4. Kiểm tra lịch học trong "Lịch của tôi"

#### 4. Điểm danh
1. Đến phòng tập
2. Quét mã QR hoặc nhận diện khuôn mặt
3. Xác nhận điểm danh thành công

#### 5. Xem lịch sử
1. Vào "Dashboard" cá nhân
2. Xem:
   - Lịch sử điểm danh
   - Gói tập đang hoạt động
   - Lịch học đã đăng ký
   - Lịch sử thanh toán

### Cho Huấn luyện viên (Trainer)

#### 1. Quản lý lớp học
1. Vào "Lớp học của tôi"
2. Xem danh sách học viên
3. Điểm danh cho lớp
4. Cập nhật thông tin buổi học

#### 2. Quản lý lịch dạy
1. Vào "Lịch làm việc"
2. Xem lịch dạy trong tuần/tháng
3. Đăng ký lịch nghỉ phép

#### 3. Xem báo cáo lương
1. Vào "Bảng lương"
2. Chọn tháng cần xem
3. Xem chi tiết:
   - Số buổi dạy
   - Hoa hồng
   - Tổng thu nhập

### Hướng dẫn sử dụng tính năng

#### Thanh toán VNPay
1. Chọn gói tập/dịch vụ
2. Click "Thanh toán VNPay"
3. Chuyển đến trang VNPay
4. Chọn ngân hàng và thanh toán
5. Hệ thống tự động cập nhật sau khi thanh toán thành công

#### Nhận diện khuôn mặt
1. Lần đầu: Đăng ký khuôn mặt tại quầy
2. Điểm danh:
   - Đứng trước camera
   - Đợi hệ thống nhận diện
   - Xác nhận điểm danh thành công

#### Đặt lịch với HLV
1. Vào "Đặt lịch HLV"
2. Chọn HLV và thời gian
3. Xác nhận đặt lịch
4. Nhận thông báo qua email

### Troubleshooting

#### Không thể đăng nhập
- Kiểm tra tên đăng nhập và mật khẩu
- Click "Quên mật khẩu" để reset
- Liên hệ admin nếu tài khoản bị khóa

#### Thanh toán thất bại
- Kiểm tra số dư tài khoản
- Thử lại sau vài phút
- Liên hệ support nếu bị trừ tiền nhưng không nhận được dịch vụ

#### Không nhận được email
- Kiểm tra folder Spam
- Cập nhật email trong profile
- Liên hệ admin để kiểm tra

## 🧪 Testing

### Running Tests
```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true

# Run specific test category
dotnet test --filter Category=Unit
```

### Test Structure
```
GymManagement.Tests/
├── Unit/
│   ├── Services/
│   ├── Controllers/
│   └── Helpers/
├── Integration/
│   ├── Database/
│   └── API/
└── TestUtilities/
```

## 🤝 Đóng góp

1. Fork repository
2. Tạo feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to branch (`git push origin feature/AmazingFeature`)
5. Tạo Pull Request

## 📄 License

Dự án này được phân phối dưới giấy phép MIT. Xem file `LICENSE` để biết thêm chi tiết.

## 📞 Liên hệ

- **Email**: 90683814+daihoangphuc@users.noreply.github.com
- **GitHub**: [@daihoangphuc](https://github.com/daihoangphuc)

## 🙏 Acknowledgments

- [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core](https://docs.microsoft.com/en-us/ef/core/)
- [Tailwind CSS](https://tailwindcss.com/)
- [Flowbite](https://flowbite.com/)
- [Chart.js](https://www.chartjs.org/)

---

⭐ Nếu dự án này hữu ích, hãy cho một star nhé!
