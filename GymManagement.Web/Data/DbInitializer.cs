using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Web.Data
{
    public static class DbInitializer
    {
        public static async Task InitializeAsync(GymDbContext context, IAuthService authService)
        {
            // Ensure database is created
            await context.Database.EnsureCreatedAsync();

            // Check if data already exists
            if (await context.VaiTros.AnyAsync())
                return; // Database has been seeded

            // Seed VaiTro (Identity Roles) - Simplified to 3 roles
            var roleNames = new[] { "Admin", "Trainer", "Member" };
            var roleDescriptions = new[]
            {
                "Quản trị viên hệ thống (bao gồm Manager và Staff)",
                "Huấn luyện viên",
                "Thành viên"
            };

            var vaiTros = new List<VaiTro>();
            for (int i = 0; i < roleNames.Length; i++)
            {
                var role = new VaiTro
                {
                    TenVaiTro = roleNames[i],
                    MoTa = roleDescriptions[i]
                };

                context.VaiTros.Add(role);
                vaiTros.Add(role);
            }
            await context.SaveChangesAsync();

            // Seed NguoiDung
            var nguoiDungs = new[]
            {
                new NguoiDung
                {
                    LoaiNguoiDung = "ADMIN",
                    Ho = "Nguyễn",
                    Ten = "Văn Admin",
                    GioiTinh = "Nam",
                    NgaySinh = new DateOnly(1990, 1, 1),
                    SoDienThoai = "0123456789",
                    Email = "admin@gym.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "ACTIVE"
                },
                new NguoiDung
                {
                    LoaiNguoiDung = "HLV",
                    Ho = "Trần",
                    Ten = "Thị Hương",
                    GioiTinh = "Nữ",
                    NgaySinh = new DateOnly(1985, 5, 15),
                    SoDienThoai = "0987654321",
                    Email = "huong.trainer@gym.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "ACTIVE"
                },
                new NguoiDung
                {
                    LoaiNguoiDung = "HLV",
                    Ho = "Lê",
                    Ten = "Minh Tuấn",
                    GioiTinh = "Nam",
                    NgaySinh = new DateOnly(1988, 8, 20),
                    SoDienThoai = "0912345678",
                    Email = "tuan.trainer@gym.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "ACTIVE"
                },
                new NguoiDung
                {
                    LoaiNguoiDung = "THANHVIEN",
                    Ho = "Phạm",
                    Ten = "Văn Nam",
                    GioiTinh = "Nam",
                    NgaySinh = new DateOnly(1995, 3, 10),
                    SoDienThoai = "0901234567",
                    Email = "nam.member@gmail.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Now),
                    TrangThai = "ACTIVE"
                }
            };
            await context.NguoiDungs.AddRangeAsync(nguoiDungs);
            await context.SaveChangesAsync();

            // Seed GoiTap
            var goiTaps = new[]
            {
                new GoiTap
                {
                    TenGoi = "Gói Cơ Bản",
                    ThoiHanThang = 1,
                    SoBuoiToiDa = 20,
                    Gia = 500000,
                    MoTa = "Gói tập cơ bản 1 tháng, tối đa 20 buổi"
                },
                new GoiTap
                {
                    TenGoi = "Gói Tiêu Chuẩn",
                    ThoiHanThang = 3,
                    SoBuoiToiDa = 60,
                    Gia = 1200000,
                    MoTa = "Gói tập 3 tháng, tối đa 60 buổi"
                },
                new GoiTap
                {
                    TenGoi = "Gói Premium",
                    ThoiHanThang = 6,
                    SoBuoiToiDa = null, // Không giới hạn
                    Gia = 2000000,
                    MoTa = "Gói tập 6 tháng, không giới hạn số buổi"
                },
                new GoiTap
                {
                    TenGoi = "Gói VIP",
                    ThoiHanThang = 12,
                    SoBuoiToiDa = null, // Không giới hạn
                    Gia = 3500000,
                    MoTa = "Gói tập VIP 12 tháng, không giới hạn + PT cá nhân"
                }
            };
            await context.GoiTaps.AddRangeAsync(goiTaps);
            await context.SaveChangesAsync();

            // Seed LopHoc
            var lopHocs = new[]
            {
                new LopHoc
                {
                    TenLop = "Yoga Buổi Sáng",
                    HlvId = nguoiDungs[1].NguoiDungId, // Trần Thị Hương
                    SucChua = 15,
                    GioBatDau = new TimeOnly(7, 0),
                    GioKetThuc = new TimeOnly(8, 0),
                    ThuTrongTuan = "Monday,Wednesday,Friday",
                    GiaTuyChinh = 200000,
                    TrangThai = "OPEN",
                    MoTa = "Lớp yoga thư giãn buổi sáng, giúp cải thiện sức khỏe và tinh thần",
                    ThoiLuong = 60
                },
                new LopHoc
                {
                    TenLop = "Gym Tăng Cơ",
                    HlvId = nguoiDungs[2].NguoiDungId, // Lê Minh Tuấn
                    SucChua = 10,
                    GioBatDau = new TimeOnly(18, 0),
                    GioKetThuc = new TimeOnly(19, 30),
                    ThuTrongTuan = "Tuesday,Thursday,Saturday",
                    GiaTuyChinh = 300000,
                    TrangThai = "OPEN",
                    MoTa = "Lớp tập gym tăng cơ bắp với huấn luyện viên chuyên nghiệp",
                    ThoiLuong = 90
                },
                new LopHoc
                {
                    TenLop = "Cardio Buổi Tối",
                    HlvId = nguoiDungs[1].NguoiDungId, // Trần Thị Hương
                    SucChua = 20,
                    GioBatDau = new TimeOnly(19, 0),
                    GioKetThuc = new TimeOnly(20, 0),
                    ThuTrongTuan = "Monday,Tuesday,Wednesday,Thursday,Friday",
                    GiaTuyChinh = 150000,
                    TrangThai = "OPEN",
                    MoTa = "Lớp cardio giảm cân buổi tối, phù hợp cho mọi lứa tuổi",
                    ThoiLuong = 60
                }
            };
            await context.LopHocs.AddRangeAsync(lopHocs);
            await context.SaveChangesAsync();

            // Seed KhuyenMai
            var khuyenMais = new[]
            {
                new KhuyenMai
                {
                    MaCode = "WELCOME2024",
                    MoTa = "Giảm giá 20% cho thành viên mới",
                    PhanTramGiam = 20,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Now),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Now.AddMonths(3)),
                    KichHoat = true
                },
                new KhuyenMai
                {
                    MaCode = "SUMMER2024",
                    MoTa = "Khuyến mãi mùa hè giảm 15%",
                    PhanTramGiam = 15,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Now),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Now.AddMonths(2)),
                    KichHoat = true
                }
            };
            await context.KhuyenMais.AddRangeAsync(khuyenMais);
            await context.SaveChangesAsync();

            // Seed TaiKhoan (Identity Users)
            var userAccounts = new[]
            {
                new { Username = "admin", Email = "admin@gym.com", Password = "Admin@123", Role = "Admin", NguoiDungId = nguoiDungs[0].NguoiDungId },
                new { Username = "trainer1", Email = "huong.trainer@gym.com", Password = "Trainer@123", Role = "Trainer", NguoiDungId = nguoiDungs[1].NguoiDungId },
                new { Username = "trainer2", Email = "tuan.trainer@gym.com", Password = "Trainer@123", Role = "Trainer", NguoiDungId = nguoiDungs[2].NguoiDungId },
                new { Username = "member1", Email = "nam.member@gmail.com", Password = "Member@123", Role = "Member", NguoiDungId = nguoiDungs[3].NguoiDungId }
            };

            foreach (var account in userAccounts)
            {
                var user = new TaiKhoan
                {
                    TenDangNhap = account.Username,
                    Email = account.Email,
                    NguoiDungId = account.NguoiDungId,
                    KichHoat = true,
                    EmailXacNhan = true
                };

                var result = await authService.CreateUserAsync(user, account.Password);
                if (result)
                {
                    await authService.AssignRoleAsync(user.Id, account.Role);
                }
            }
        }
    }
}
