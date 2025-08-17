using FluentAssertions;
using GymManagement.Web.Controllers;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using GymManagement.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymManagement.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration Tests cho DiemDanhController
    /// HOÀN TOÀN AN TOÀN - SỬ DỤNG IN-MEMORY DATABASE
    /// </summary>
    public class DiemDanhControllerTests : IDisposable
    {
        private readonly GymDbContext _context;
        private readonly DiemDanhController _controller;
        private readonly Mock<IDiemDanhService> _mockDiemDanhService;
        private readonly Mock<INguoiDungService> _mockNguoiDungService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IUserSessionService> _mockUserSessionService;

        public DiemDanhControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            
            // Setup mock services
            _mockDiemDanhService = new Mock<IDiemDanhService>();
            _mockNguoiDungService = new Mock<INguoiDungService>();
            _mockAuthService = new Mock<IAuthService>();
            _mockUserSessionService = new Mock<IUserSessionService>();
            
            var mockLogger = new Mock<ILogger<DiemDanhController>>();
            
            _controller = new DiemDanhController(
                _mockDiemDanhService.Object,
                _mockNguoiDungService.Object,
                _mockAuthService.Object,
                _mockUserSessionService.Object,
                mockLogger.Object
            );
            
            SetupMockServices();
        }

        [Fact]
        public async Task Index_WithAdminRole_ShouldReturnAllAttendanceRecords()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupAdminUser();
            
            var allAttendance = _context.DiemDanhs.ToList();
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<DiemDanh>;
            model.Should().NotBeNull();
            model!.Count().Should().Be(allAttendance.Count);
        }

        [Fact]
        public async Task Index_WithTrainerRole_ShouldReturnFilteredAttendanceRecords()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id"); // Trainer 1
            
            var trainer1Attendance = _context.DiemDanhs
                .Where(d => d.LopHoc != null && d.LopHoc.HlvId == 1)
                .ToList();
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<DiemDanh>;
            model.Should().NotBeNull();
            
            // Trainer should only see attendance from their own classes
            model!.Count().Should().Be(trainer1Attendance.Count);
            model.All(d => d.LopHoc?.HlvId == 1).Should().BeTrue();
        }

        [Fact]
        public async Task Index_WithTrainer2_ShouldOnlySeeTheirClassAttendance()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(2, "trainer-2-test-id"); // Trainer 2
            
            var trainer2Attendance = _context.DiemDanhs
                .Where(d => d.LopHoc != null && d.LopHoc.HlvId == 2)
                .ToList();
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<DiemDanh>;
            model.Should().NotBeNull();
            
            // Trainer 2 should only see attendance from their own classes
            model!.Count().Should().Be(trainer2Attendance.Count);
            model.All(d => d.LopHoc?.HlvId == 2).Should().BeTrue();
        }

        [Fact]
        public async Task Index_WithUnauthorizedRole_ShouldReturnForbid()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupCustomerUser();
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task Index_TrainerCannotSeeOtherTrainerAttendance()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id"); // Trainer 1
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<DiemDanh>;
            model.Should().NotBeNull();
            
            // Verify Trainer 1 cannot see Trainer 2's attendance
            var trainer2Attendance = model!.Where(d => d.LopHoc?.HlvId == 2);
            trainer2Attendance.Should().BeEmpty();
        }

        [Fact]
        public async Task Index_WithNoAttendanceData_ShouldReturnEmptyList()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Clear attendance data
            _context.DiemDanhs.RemoveRange(_context.DiemDanhs);
            await _context.SaveChangesAsync();
            
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<DiemDanh>;
            model.Should().NotBeNull();
            model!.Should().BeEmpty();
        }

        [Fact]
        public async Task Index_WithTrainerHavingNoClasses_ShouldReturnEmptyList()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Setup trainer with no classes (trainer ID 999)
            SetupTrainerUser(999, "trainer-999-test-id");
            
            // Act
            var result = await _controller.Index();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<DiemDanh>;
            model.Should().NotBeNull();
            model!.Should().BeEmpty();
        }

        [Fact]
        public async Task Index_SecurityTest_TrainerCannotAccessOtherTrainerData()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Test with Trainer 1
            SetupTrainerUser(1, "trainer-1-test-id");
            var result1 = await _controller.Index();
            var viewResult1 = result1 as ViewResult;
            var model1 = viewResult1!.Model as IEnumerable<DiemDanh>;
            
            // Test with Trainer 2
            SetupTrainerUser(2, "trainer-2-test-id");
            var result2 = await _controller.Index();
            var viewResult2 = result2 as ViewResult;
            var model2 = viewResult2!.Model as IEnumerable<DiemDanh>;
            
            // Assert
            model1.Should().NotBeNull();
            model2.Should().NotBeNull();
            
            // Verify no overlap between trainer data
            var trainer1ClassIds = model1!.Select(d => d.LopHocId).Distinct();
            var trainer2ClassIds = model2!.Select(d => d.LopHocId).Distinct();
            
            trainer1ClassIds.Should().NotIntersectWith(trainer2ClassIds);
        }

        [Fact]
        public async Task Index_PerformanceTest_WithLargeDataset()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Add more attendance records for performance testing
            var additionalAttendance = new List<DiemDanh>();
            for (int i = 1; i <= 100; i++)
            {
                additionalAttendance.Add(new DiemDanh
                {
                    DiemDanhId = 100 + i,
                    ThanhVienId = 101,
                    LopHocId = 1, // Trainer 1's class
                    ThoiGian = DateTime.Today.AddDays(-i),
                    ThoiGianCheckIn = DateTime.Today.AddDays(-i).AddHours(9),
                    TrangThai = "Present"
                });
            }
            
            _context.DiemDanhs.AddRange(additionalAttendance);
            await _context.SaveChangesAsync();
            
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _controller.Index();
            stopwatch.Stop();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
        }

        private void SetupMockServices()
        {
            // Setup mock services to return data from In-Memory context
            _mockDiemDanhService.Setup(x => x.GetAllAsync())
                .ReturnsAsync(() => _context.DiemDanhs
                    .Select(d => new DiemDanh
                    {
                        DiemDanhId = d.DiemDanhId,
                        ThanhVienId = d.ThanhVienId,
                        LopHocId = d.LopHocId,
                        ThoiGian = d.ThoiGian,
                        ThoiGianCheckIn = d.ThoiGianCheckIn,
                        TrangThai = d.TrangThai,
                        LopHoc = _context.LopHocs.FirstOrDefault(l => l.LopHocId == d.LopHocId)
                    })
                    .ToList());
            
            _mockAuthService.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.TaiKhoans.FirstOrDefault(t => t.Id == id));
        }

        private void SetupAdminUser()
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(true);
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(false);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns("admin-test-id");
            _mockUserSessionService.Setup(x => x.GetUserName()).Returns("admin@test.com");
        }

        private void SetupTrainerUser(int nguoiDungId, string taiKhoanId)
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(true);
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(false);
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns(nguoiDungId);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns(taiKhoanId);
            _mockUserSessionService.Setup(x => x.GetUserName()).Returns($"trainer{nguoiDungId}@test.com");
        }

        private void SetupCustomerUser()
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(false);
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(false);
            _mockUserSessionService.Setup(x => x.IsInRole("Customer")).Returns(true);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns("customer-test-id");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
