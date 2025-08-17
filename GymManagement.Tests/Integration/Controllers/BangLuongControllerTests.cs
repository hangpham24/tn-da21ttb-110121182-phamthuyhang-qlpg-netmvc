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
    /// Integration Tests cho BangLuongController
    /// HOÀN TOÀN AN TOÀN - SỬ DỤNG IN-MEMORY DATABASE
    /// </summary>
    public class BangLuongControllerTests : IDisposable
    {
        private readonly GymDbContext _context;
        private readonly BangLuongController _controller;
        private readonly Mock<IBangLuongService> _mockBangLuongService;
        private readonly Mock<INguoiDungService> _mockNguoiDungService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IPdfExportService> _mockPdfExportService;
        private readonly Mock<IAuditLogService> _mockAuditLogService;
        private readonly Mock<IUserSessionService> _mockUserSessionService;

        public BangLuongControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            
            // Setup mock services
            _mockBangLuongService = new Mock<IBangLuongService>();
            _mockNguoiDungService = new Mock<INguoiDungService>();
            _mockAuthService = new Mock<IAuthService>();
            _mockPdfExportService = new Mock<IPdfExportService>();
            _mockAuditLogService = new Mock<IAuditLogService>();
            _mockUserSessionService = new Mock<IUserSessionService>();
            
            var mockLogger = new Mock<ILogger<BangLuongController>>();
            
            _controller = new BangLuongController(
                _mockBangLuongService.Object,
                _mockNguoiDungService.Object,
                _mockAuthService.Object,
                _mockPdfExportService.Object,
                _mockAuditLogService.Object,
                _mockUserSessionService.Object,
                mockLogger.Object
            );
            
            SetupMockServices();
        }

        [Fact]
        public async Task MySalary_WithValidTrainer_ShouldReturnOwnSalaryData()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var trainer1Salaries = _context.BangLuongs.Where(b => b.HlvId == 1).ToList();
            
            // Act
            var result = await _controller.MySalary();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<BangLuong>;
            model.Should().NotBeNull();
            model!.Count().Should().Be(trainer1Salaries.Count);
            model.All(s => s.HlvId == 1).Should().BeTrue();
        }

        [Fact]
        public async Task MySalary_WithDifferentTrainer_ShouldReturnOnlyOwnSalary()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(2, "trainer-2-test-id"); // Trainer 2
            
            var trainer2Salaries = _context.BangLuongs.Where(b => b.HlvId == 2).ToList();
            
            // Act
            var result = await _controller.MySalary();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<BangLuong>;
            model.Should().NotBeNull();
            model!.Count().Should().Be(trainer2Salaries.Count);
            model.All(s => s.HlvId == 2).Should().BeTrue();
            
            // Verify cannot see other trainer's salary
            model.Any(s => s.HlvId == 1).Should().BeFalse();
        }

        [Fact]
        public async Task MySalary_WithUnauthorizedRole_ShouldReturnForbid()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupCustomerUser();
            
            // Act
            var result = await _controller.MySalary();
            
            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task MySalary_WithInvalidUser_ShouldRedirectToLogin()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupInvalidUser();
            
            // Act
            var result = await _controller.MySalary();
            
            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("Auth");
        }

        [Fact]
        public async Task ExportMySalaryPdf_WithValidTrainerAndMonth_ShouldReturnPdfFile()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            var mockPdfBytes = new byte[] { 1, 2, 3, 4, 5 };
            
            _mockPdfExportService.Setup(x => x.GenerateSalaryReportAsync(It.IsAny<BangLuong>(), It.IsAny<BangLuongService.CommissionBreakdown>()))
                .ReturnsAsync(mockPdfBytes);
            
            // Act
            var result = await _controller.ExportMySalaryPdf(currentMonth);
            
            // Assert
            result.Should().BeOfType<FileResult>();
            var fileResult = result as FileResult;
            fileResult!.ContentType.Should().Be("application/pdf");
            fileResult.FileDownloadName.Should().Be($"BangLuong_{currentMonth}.pdf");
        }

        [Fact]
        public async Task ExportMySalaryPdf_WithNonExistentMonth_ShouldRedirectWithError()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var nonExistentMonth = "2020-01"; // Month with no salary data
            
            // Act
            var result = await _controller.ExportMySalaryPdf(nonExistentMonth);
            
            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("MySalary");
        }

        [Fact]
        public async Task ExportMySalaryPdf_WithUnauthorizedRole_ShouldReturnForbid()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupCustomerUser();
            
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            
            // Act
            var result = await _controller.ExportMySalaryPdf(currentMonth);
            
            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public async Task ExportMySalaryPdf_WithInvalidUser_ShouldRedirectToLogin()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupInvalidUser();
            
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            
            // Act
            var result = await _controller.ExportMySalaryPdf(currentMonth);
            
            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("Auth");
        }

        [Fact]
        public async Task SecurityTest_TrainerCannotAccessOtherTrainerSalary()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Test with Trainer 1
            SetupTrainerUser(1, "trainer-1-test-id");
            var result1 = await _controller.MySalary();
            var viewResult1 = result1 as ViewResult;
            var model1 = viewResult1!.Model as IEnumerable<BangLuong>;
            
            // Test with Trainer 2
            SetupTrainerUser(2, "trainer-2-test-id");
            var result2 = await _controller.MySalary();
            var viewResult2 = result2 as ViewResult;
            var model2 = viewResult2!.Model as IEnumerable<BangLuong>;
            
            // Assert
            model1.Should().NotBeNull();
            model2.Should().NotBeNull();
            
            // Verify no overlap between trainer salary data
            var trainer1SalaryIds = model1!.Select(s => s.BangLuongId).ToList();
            var trainer2SalaryIds = model2!.Select(s => s.BangLuongId).ToList();
            
            trainer1SalaryIds.Should().NotIntersectWith(trainer2SalaryIds);
            
            // Verify each trainer only sees their own data
            model1.All(s => s.HlvId == 1).Should().BeTrue();
            model2.All(s => s.HlvId == 2).Should().BeTrue();
        }

        [Fact]
        public async Task SecurityTest_TrainerCannotExportOtherTrainerSalaryPdf()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Setup Trainer 1 trying to access their own salary
            SetupTrainerUser(1, "trainer-1-test-id");
            var currentMonth = DateTime.Now.ToString("yyyy-MM");
            
            var mockPdfBytes = new byte[] { 1, 2, 3, 4, 5 };
            _mockPdfExportService.Setup(x => x.GenerateSalaryReportAsync(It.IsAny<BangLuong>(), It.IsAny<BangLuongService.CommissionBreakdown>()))
                .ReturnsAsync(mockPdfBytes);
            
            // Act - Trainer 1 accessing their own salary (should work)
            var result = await _controller.ExportMySalaryPdf(currentMonth);
            
            // Assert
            result.Should().BeOfType<FileResult>();
            
            // Verify the service was called with Trainer 1's salary only
            _mockBangLuongService.Verify(x => x.GetByTrainerAndMonthAsync(1, currentMonth), Times.Once);
            _mockBangLuongService.Verify(x => x.GetByTrainerAndMonthAsync(2, currentMonth), Times.Never);
        }

        [Fact]
        public async Task PerformanceTest_MySalaryWithLargeDataset()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Add more salary records for performance testing
            var additionalSalaries = new List<BangLuong>();
            for (int i = 1; i <= 50; i++)
            {
                additionalSalaries.Add(new BangLuong
                {
                    BangLuongId = 100 + i,
                    HlvId = 1, // Trainer 1
                    Thang = DateTime.Now.AddMonths(-i).ToString("yyyy-MM"),
                    LuongCoBan = 5000000,
                    TienHoaHong = 1000000,
                    NgayThanhToan = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-i)),
                    NgayTao = DateTime.UtcNow.AddDays(-i)
                });
            }
            
            _context.BangLuongs.AddRange(additionalSalaries);
            await _context.SaveChangesAsync();
            
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _controller.MySalary();
            stopwatch.Stop();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000); // Should complete within 1 second
            
            var viewResult = result as ViewResult;
            var model = viewResult!.Model as IEnumerable<BangLuong>;
            model!.Count().Should().BeGreaterThan(50); // Should include all salary records
        }

        private void SetupMockServices()
        {
            // Setup mock services to return data from In-Memory context
            _mockBangLuongService.Setup(x => x.GetByTrainerIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int trainerId) => _context.BangLuongs.Where(b => b.HlvId == trainerId).ToList());
            
            _mockBangLuongService.Setup(x => x.GetByTrainerAndMonthAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int trainerId, string month) => 
                    _context.BangLuongs.FirstOrDefault(b => b.HlvId == trainerId && b.Thang == month));
            
            _mockBangLuongService.Setup(x => x.CalculateDetailedCommissionAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync(new BangLuongService.CommissionBreakdown
                {
                    PackageCommission = 500000,
                    ClassCommission = 300000,
                    PersonalCommission = 200000,
                    PerformanceBonus = 0,
                    AttendanceBonus = 0
                });
            
            _mockAuthService.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.TaiKhoans.FirstOrDefault(t => t.Id == id));
        }

        private void SetupTrainerUser(int nguoiDungId, string taiKhoanId)
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(true);
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(false);
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns(nguoiDungId);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns(taiKhoanId);
            _mockUserSessionService.Setup(x => x.GetUserName()).Returns($"trainer{nguoiDungId}@test.com");
        }

        private void SetupInvalidUser()
        {
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns((string?)null);
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns((int?)null);
        }

        private void SetupCustomerUser()
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(false);
            _mockUserSessionService.Setup(x => x.IsInRole("Customer")).Returns(true);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns("customer-test-id");
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
