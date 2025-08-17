using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;

namespace GymManagement.Tests.TestHelpers
{
    /// <summary>
    /// Seeder để tạo test data trong In-Memory Database
    /// HOÀN TOÀN AN TOÀN - CHỈ TẠO DATA TRONG MEMORY
    /// </summary>
    public static class TestDataSeeder
    {
        /// <summary>
        /// Seed complete test data với Admin, Trainers, Students, Classes
        /// </summary>
        public static async Task SeedCompleteTestDataAsync(GymDbContext context)
        {
            // Ensure In-Memory database
            TestDbContextFactory.EnsureInMemoryDatabase(context);
            
            // Clear existing data
            await TestDbContextFactory.CleanupDatabaseAsync(context);
            
            // Seed Roles
            await SeedRolesAsync(context);
            
            // Seed Users
            await SeedUsersAsync(context);
            
            // Seed Classes
            await SeedClassesAsync(context);
            
            // Seed Registrations
            await SeedRegistrationsAsync(context);
            
            // Seed Salaries
            await SeedSalariesAsync(context);
            
            // Seed Attendance
            await SeedAttendanceAsync(context);
            
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Roles
        /// </summary>
        public static async Task SeedRolesAsync(GymDbContext context)
        {
            var roles = new[]
            {
                new VaiTro { Id = "admin-role-id", TenVaiTro = "Admin", MoTa = "Administrator" },
                new VaiTro { Id = "trainer-role-id", TenVaiTro = "Trainer", MoTa = "Trainer/HLV" },
                new VaiTro { Id = "customer-role-id", TenVaiTro = "Customer", MoTa = "Customer" }
            };

            context.VaiTros.AddRange(roles);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Users (Admin, Trainers, Students)
        /// </summary>
        public static async Task SeedUsersAsync(GymDbContext context)
        {
            // Admin Account
            var adminAccount = new TaiKhoan
            {
                Id = "admin-test-id",
                TenDangNhap = "admin@test.com",
                Email = "admin@test.com",
                MatKhauHash = "hashed-password",
                Salt = "test-salt",
                NgayTao = DateTime.UtcNow
            };
            context.TaiKhoans.Add(adminAccount);

            // Admin User
            var adminUser = new NguoiDung
            {
                NguoiDungId = 999,
                Ho = "Admin",
                Ten = "Test",
                Email = "admin@test.com",
                SoDienThoai = "0000000000",
                LoaiNguoiDung = "ADMIN",
                TrangThai = "ACTIVE",
                NgayTao = DateTime.UtcNow
            };
            context.NguoiDungs.Add(adminUser);

            // Admin Role Assignment
            var adminRole = new TaiKhoanVaiTro
            {
                TaiKhoanId = "admin-test-id",
                VaiTroId = "admin-role-id",
                NgayGan = DateTime.UtcNow
            };
            context.TaiKhoanVaiTros.Add(adminRole);

            // Trainer 1
            var trainer1Account = new TaiKhoan
            {
                Id = "trainer-1-test-id",
                TenDangNhap = "trainer1@test.com",
                Email = "trainer1@test.com",
                MatKhauHash = "hashed-password",
                Salt = "test-salt",
                NgayTao = DateTime.UtcNow,
                NguoiDungId = 1
            };
            context.TaiKhoans.Add(trainer1Account);

            var trainer1User = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Trainer",
                Ten = "One",
                Email = "trainer1@test.com",
                SoDienThoai = "0111111111",
                LoaiNguoiDung = "HLV",
                TrangThai = "ACTIVE",
                NgayTao = DateTime.UtcNow
            };
            context.NguoiDungs.Add(trainer1User);

            var trainer1Role = new TaiKhoanVaiTro
            {
                TaiKhoanId = "trainer-1-test-id",
                VaiTroId = "trainer-role-id",
                NgayGan = DateTime.UtcNow
            };
            context.TaiKhoanVaiTros.Add(trainer1Role);

            // Trainer 2
            var trainer2Account = new TaiKhoan
            {
                Id = "trainer-2-test-id",
                TenDangNhap = "trainer2@test.com",
                Email = "trainer2@test.com",
                MatKhauHash = "hashed-password",
                Salt = "test-salt",
                NgayTao = DateTime.UtcNow,
                NguoiDungId = 2
            };
            context.TaiKhoans.Add(trainer2Account);

            var trainer2User = new NguoiDung
            {
                NguoiDungId = 2,
                Ho = "Trainer",
                Ten = "Two",
                Email = "trainer2@test.com",
                SoDienThoai = "0222222222",
                LoaiNguoiDung = "HLV",
                TrangThai = "ACTIVE",
                NgayTao = DateTime.UtcNow
            };
            context.NguoiDungs.Add(trainer2User);

            var trainer2Role = new TaiKhoanVaiTro
            {
                TaiKhoanId = "trainer-2-test-id",
                VaiTroId = "trainer-role-id",
                NgayGan = DateTime.UtcNow
            };
            context.TaiKhoanVaiTros.Add(trainer2Role);

            // Students
            for (int i = 1; i <= 5; i++)
            {
                var studentAccount = new TaiKhoan
                {
                    Id = $"student-{i}-test-id",
                    TenDangNhap = $"student{i}@test.com",
                    Email = $"student{i}@test.com",
                    MatKhauHash = "hashed-password",
                    Salt = "test-salt",
                    NgayTao = DateTime.UtcNow,
                    NguoiDungId = 100 + i
                };
                context.TaiKhoans.Add(studentAccount);

                var studentUser = new NguoiDung
                {
                    NguoiDungId = 100 + i,
                    Ho = "Student",
                    Ten = $"Number{i}",
                    Email = $"student{i}@test.com",
                    SoDienThoai = $"03{i:D8}",
                    LoaiNguoiDung = "CUSTOMER",
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.UtcNow
                };
                context.NguoiDungs.Add(studentUser);

                var studentRole = new TaiKhoanVaiTro
                {
                    TaiKhoanId = $"student-{i}-test-id",
                    VaiTroId = "customer-role-id",
                    NgayGan = DateTime.UtcNow
                };
                context.TaiKhoanVaiTros.Add(studentRole);
            }

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Classes
        /// </summary>
        public static async Task SeedClassesAsync(GymDbContext context)
        {
            var classes = new[]
            {
                new LopHoc
                {
                    LopHocId = 1,
                    TenLop = "Yoga Class - Trainer 1",
                    MoTa = "Yoga class taught by Trainer 1",
                    HlvId = 1, // Trainer 1
                    SucChua = 20,
                    TrangThai = "OPEN",
                    GioBatDau = new TimeOnly(9, 0),
                    GioKetThuc = new TimeOnly(10, 0),
                    ThuTrongTuan = "Monday,Wednesday,Friday"
                },
                new LopHoc
                {
                    LopHocId = 2,
                    TenLop = "Fitness Class - Trainer 1",
                    MoTa = "Fitness class taught by Trainer 1",
                    HlvId = 1, // Trainer 1
                    SucChua = 15,
                    TrangThai = "OPEN",
                    GioBatDau = new TimeOnly(14, 0),
                    GioKetThuc = new TimeOnly(15, 0),
                    ThuTrongTuan = "Tuesday,Thursday"
                },
                new LopHoc
                {
                    LopHocId = 3,
                    TenLop = "Boxing Class - Trainer 2",
                    MoTa = "Boxing class taught by Trainer 2",
                    HlvId = 2, // Trainer 2
                    SucChua = 10,
                    TrangThai = "OPEN",
                    GioBatDau = new TimeOnly(18, 0),
                    GioKetThuc = new TimeOnly(19, 0),
                    ThuTrongTuan = "Monday,Wednesday,Friday"
                }
            };

            context.LopHocs.AddRange(classes);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Registrations
        /// </summary>
        public static async Task SeedRegistrationsAsync(GymDbContext context)
        {
            var registrations = new[]
            {
                // Students 1-3 in Trainer 1's classes
                new DangKy
                {
                    DangKyId = 1,
                    NguoiDungId = 101,
                    LopHocId = 1,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    TrangThai = "ACTIVE"
                },
                new DangKy
                {
                    DangKyId = 2,
                    NguoiDungId = 102,
                    LopHocId = 1,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    TrangThai = "ACTIVE"
                },
                new DangKy
                {
                    DangKyId = 3,
                    NguoiDungId = 103,
                    LopHocId = 2,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    TrangThai = "ACTIVE"
                },
                // Students 4-5 in Trainer 2's class
                new DangKy
                {
                    DangKyId = 4,
                    NguoiDungId = 104,
                    LopHocId = 3,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    TrangThai = "ACTIVE"
                },
                new DangKy
                {
                    DangKyId = 5,
                    NguoiDungId = 105,
                    LopHocId = 3,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(30)),
                    TrangThai = "ACTIVE"
                }
            };

            context.DangKys.AddRange(registrations);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Salaries
        /// </summary>
        public static async Task SeedSalariesAsync(GymDbContext context)
        {
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var lastMonth = DateTime.Now.AddMonths(-1).ToString("yyyy-MM");

            var salaries = new[]
            {
                new BangLuong
                {
                    BangLuongId = 1,
                    HlvId = 1,
                    Thang = currentMonth,
                    LuongCoBan = 5000000,
                    TienHoaHong = 1000000,
                    NgayThanhToan = null, // Chưa thanh toán
                    NgayTao = DateTime.UtcNow
                },
                new BangLuong
                {
                    BangLuongId = 2,
                    HlvId = 1,
                    Thang = lastMonth,
                    LuongCoBan = 5000000,
                    TienHoaHong = 800000,
                    NgayThanhToan = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)), // Đã thanh toán
                    NgayTao = DateTime.UtcNow.AddDays(-30)
                },
                new BangLuong
                {
                    BangLuongId = 3,
                    HlvId = 2,
                    Thang = currentMonth,
                    LuongCoBan = 4500000,
                    TienHoaHong = 900000,
                    NgayThanhToan = null, // Chưa thanh toán
                    NgayTao = DateTime.UtcNow
                }
            };

            context.BangLuongs.AddRange(salaries);
            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Seed Attendance
        /// </summary>
        public static async Task SeedAttendanceAsync(GymDbContext context)
        {
            var attendances = new[]
            {
                new DiemDanh
                {
                    DiemDanhId = 1,
                    ThanhVienId = 101,
                    LopHocId = 1,
                    ThoiGian = DateTime.Today,
                    ThoiGianCheckIn = DateTime.Today.AddHours(9),
                    TrangThai = "Present"
                },
                new DiemDanh
                {
                    DiemDanhId = 2,
                    ThanhVienId = 102,
                    LopHocId = 1,
                    ThoiGian = DateTime.Today,
                    ThoiGianCheckIn = DateTime.Today.AddHours(9),
                    TrangThai = "Present"
                },
                new DiemDanh
                {
                    DiemDanhId = 3,
                    ThanhVienId = 104,
                    LopHocId = 3,
                    ThoiGian = DateTime.Today,
                    ThoiGianCheckIn = DateTime.Today.AddHours(18),
                    TrangThai = "Present"
                }
            };

            context.DiemDanhs.AddRange(attendances);
            await context.SaveChangesAsync();
        }
    }
}
