using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymManagement.Tests.TestUtilities
{
    /// <summary>
    /// Helper class for Check-in/Check-out feature testing
    /// </summary>
    public static class CheckInTestHelper
    {
        /// <summary>
        /// Creates an in-memory database context for testing
        /// </summary>
        public static GymDbContext CreateInMemoryContext(string databaseName = null)
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
                .Options;

            var context = new GymDbContext(options);

            // Seed test data
            SeedTestData(context);

            return context;
        }

        /// <summary>
        /// Seeds test data for check-in/check-out testing
        /// </summary>
        public static void SeedTestData(GymDbContext context)
        {
            // Clear existing data
            context.Database.EnsureDeleted();
            context.Database.EnsureCreated();

            // Seed test members
            var members = new List<NguoiDung>
            {
                new NguoiDung 
                { 
                    NguoiDungId = 1, 
                    LoaiNguoiDung = "THANHVIEN", 
                    Ho = "Nguyễn", 
                    Ten = "Văn A", 
                    SoDienThoai = "0123456789",
                    Email = "nguyenvana@example.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today.AddDays(-30)),
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now.AddDays(-30)
                },
                new NguoiDung 
                { 
                    NguoiDungId = 2, 
                    LoaiNguoiDung = "THANHVIEN", 
                    Ho = "Trần", 
                    Ten = "Thị B", 
                    SoDienThoai = "0987654321",
                    Email = "tranthib@example.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today.AddDays(-60)),
                    TrangThai = "INACTIVE",
                    NgayTao = DateTime.Now.AddDays(-60)
                },
                new NguoiDung 
                { 
                    NguoiDungId = 3, 
                    LoaiNguoiDung = "VANGLAI", 
                    Ho = "Lê", 
                    Ten = "Văn C", 
                    SoDienThoai = "0111222333",
                    Email = "levanc@example.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now
                },
                new NguoiDung 
                { 
                    NguoiDungId = 4, 
                    LoaiNguoiDung = "THANHVIEN", 
                    Ho = "Phạm", 
                    Ten = "Văn D", 
                    SoDienThoai = "0444555666",
                    Email = "phamvand@example.com",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today.AddDays(-15)),
                    TrangThai = "ACTIVE",
                    NgayTao = DateTime.Now.AddDays(-15)
                }
            };

            context.NguoiDungs.AddRange(members);

            // Seed test packages
            var packages = new List<GoiTap>
            {
                new GoiTap { GoiTapId = 1, TenGoi = "Gói cơ bản", ThoiHanThang = 1, Gia = 500000, MoTa = "Gói tập 1 tháng" },
                new GoiTap { GoiTapId = 2, TenGoi = "Gói premium", ThoiHanThang = 3, Gia = 1200000, MoTa = "Gói tập 3 tháng" },
                new GoiTap { GoiTapId = 3, TenGoi = "Vé ngày", ThoiHanThang = 0, Gia = 50000, MoTa = "Vé tập 1 ngày" }
            };

            context.GoiTaps.AddRange(packages);

            // Seed active registrations
            var registrations = new List<DangKy>
            {
                new DangKy 
                { 
                    DangKyId = 1,
                    NguoiDungId = 1, 
                    GoiTapId = 1,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-15)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(15)),
                    TrangThai = "ACTIVE",
                    PhiDangKy = 500000,
                    NgayTao = DateTime.Now.AddDays(-15)
                },
                new DangKy 
                { 
                    DangKyId = 2,
                    NguoiDungId = 2, 
                    GoiTapId = 1,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-45)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(-15)),
                    TrangThai = "EXPIRED",
                    PhiDangKy = 500000,
                    NgayTao = DateTime.Now.AddDays(-45)
                },
                new DangKy 
                { 
                    DangKyId = 3,
                    NguoiDungId = 4, 
                    GoiTapId = 2,
                    NgayBatDau = DateOnly.FromDateTime(DateTime.Today.AddDays(-10)),
                    NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddDays(80)),
                    TrangThai = "ACTIVE",
                    PhiDangKy = 1200000,
                    NgayTao = DateTime.Now.AddDays(-10)
                }
            };

            context.DangKys.AddRange(registrations);

            // Seed face recognition data
            var faceData = new List<MauMat>
            {
                new MauMat
                {
                    MauMatId = 1,
                    NguoiDungId = 1,
                    Embedding = new byte[512], // Mock embedding data
                    NgayTao = DateTime.Now.AddDays(-30),
                    ThuatToan = "ArcFace"
                },
                new MauMat
                {
                    MauMatId = 2,
                    NguoiDungId = 4,
                    Embedding = new byte[512], // Mock embedding data
                    NgayTao = DateTime.Now.AddDays(-15),
                    ThuatToan = "ArcFace"
                }
            };

            context.MauMats.AddRange(faceData);

            context.SaveChanges();
        }

        /// <summary>
        /// Creates a mock configuration for testing
        /// </summary>
        public static Mock<IConfiguration> CreateMockConfiguration()
        {
            var mockConfig = new Mock<IConfiguration>();
            
            // Mock gym operating hours
            var mockGymSection = new Mock<IConfigurationSection>();
            mockGymSection.Setup(x => x["OperatingHours:OpenTime"]).Returns("05:00");
            mockGymSection.Setup(x => x["OperatingHours:CloseTime"]).Returns("23:00");
            mockGymSection.Setup(x => x["OperatingHours:AllowCheckInOutsideHours"]).Returns("false");
            mockGymSection.Setup(x => x["CheckIn:MaxSessionsPerDay"]).Returns("1");
            mockGymSection.Setup(x => x["CheckIn:RequireActivePackage"]).Returns("true");
            mockGymSection.Setup(x => x["CheckIn:AutoCheckOutAfterHours"]).Returns("24");

            mockConfig.Setup(x => x.GetSection("Gym")).Returns(mockGymSection.Object);

            return mockConfig;
        }

        /// <summary>
        /// Creates a mock logger for testing
        /// </summary>
        public static Mock<ILogger<T>> CreateMockLogger<T>()
        {
            return new Mock<ILogger<T>>();
        }

        /// <summary>
        /// Creates a mock unit of work with in-memory context
        /// </summary>
        public static Mock<IUnitOfWork> CreateMockUnitOfWork(GymDbContext context)
        {
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            mockUnitOfWork.Setup(x => x.Context).Returns(context);
            mockUnitOfWork.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
            return mockUnitOfWork;
        }

        /// <summary>
        /// Creates sample attendance record
        /// </summary>
        public static DiemDanh CreateSampleAttendance(int memberId, DateTime? checkInTime = null, DateTime? checkOutTime = null)
        {
            return new DiemDanh
            {
                ThanhVienId = memberId,
                ThoiGian = checkInTime ?? DateTime.Now,
                ThoiGianCheckIn = checkInTime ?? DateTime.Now,
                ThoiGianCheckOut = checkOutTime,
                KetQuaNhanDang = true,
                LoaiCheckIn = "Manual",
                TrangThai = checkOutTime.HasValue ? "Completed" : "Present",
                GhiChu = "Test attendance"
            };
        }

        /// <summary>
        /// Creates sample workout session
        /// </summary>
        public static BuoiTap CreateSampleWorkoutSession(int memberId, DateTime? startTime = null, DateTime? endTime = null)
        {
            return new BuoiTap
            {
                ThanhVienId = memberId,
                ThoiGianVao = startTime ?? DateTime.Now,
                ThoiGianRa = endTime,
                GhiChu = "Test workout session"
            };
        }

        /// <summary>
        /// Creates sample face recognition result
        /// </summary>
        public static FaceRecognitionResult CreateSampleFaceRecognitionResult(bool success, int? memberId = null, double confidence = 0.95)
        {
            return new FaceRecognitionResult
            {
                Success = success,
                MemberId = memberId,
                MemberName = memberId.HasValue ? $"Test Member {memberId}" : null,
                Confidence = confidence,
                Message = success ? "Nhận diện thành công" : "Không nhận diện được khuôn mặt"
            };
        }

        /// <summary>
        /// Asserts that two DiemDanh objects are equivalent
        /// </summary>
        public static void AssertAttendanceEquivalent(DiemDanh expected, DiemDanh actual)
        {
            Assert.Equal(expected.ThanhVienId, actual.ThanhVienId);
            Assert.Equal(expected.LoaiCheckIn, actual.LoaiCheckIn);
            Assert.Equal(expected.TrangThai, actual.TrangThai);
            Assert.Equal(expected.KetQuaNhanDang, actual.KetQuaNhanDang);
        }

        /// <summary>
        /// Asserts that two BuoiTap objects are equivalent
        /// </summary>
        public static void AssertWorkoutSessionEquivalent(BuoiTap expected, BuoiTap actual)
        {
            Assert.Equal(expected.ThanhVienId, actual.ThanhVienId);
            Assert.Equal(expected.LopHocId, actual.LopHocId);
        }

        /// <summary>
        /// Checks if time is within gym operating hours
        /// </summary>
        public static bool IsWithinOperatingHours(DateTime time, TimeSpan openTime, TimeSpan closeTime)
        {
            var timeOfDay = time.TimeOfDay;
            return timeOfDay >= openTime && timeOfDay <= closeTime;
        }

        /// <summary>
        /// Calculates session duration in a readable format
        /// </summary>
        public static string FormatSessionDuration(DateTime checkIn, DateTime checkOut)
        {
            var duration = checkOut - checkIn;
            if (duration.TotalHours >= 1)
            {
                return $"{(int)duration.TotalHours}h {duration.Minutes}m";
            }
            return $"{(int)duration.TotalMinutes}m";
        }

        /// <summary>
        /// Checks if member has active package
        /// </summary>
        public static bool HasActivePackage(List<DangKy> registrations, int memberId, DateTime checkDate)
        {
            return registrations.Any(r => 
                r.NguoiDungId == memberId && 
                r.TrangThai == "ACTIVE" &&
                DateOnly.FromDateTime(checkDate) >= r.NgayBatDau &&
                DateOnly.FromDateTime(checkDate) <= r.NgayKetThuc);
        }
    }
}
