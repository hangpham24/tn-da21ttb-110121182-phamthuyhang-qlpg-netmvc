using FluentAssertions;
using GymManagement.Web.Controllers;
using GymManagement.Web.Data;
using GymManagement.Web.Services;
using GymManagement.Web.Models.DTOs;
using GymManagement.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using System.Text.Json;
using GymManagement.Web.Models;

namespace GymManagement.Tests.Integration.Controllers
{
    /// <summary>
    /// Integration Tests cho TrainerController
    /// HOÀN TOÀN AN TOÀN - SỬ DỤNG IN-MEMORY DATABASE
    /// </summary>
    public class TrainerControllerTests : IDisposable
    {
        private readonly GymDbContext _context;
        private readonly TrainerController _controller;
        private readonly Mock<ILopHocService> _mockLopHocService;
        private readonly Mock<IBangLuongService> _mockBangLuongService;
        private readonly Mock<INguoiDungService> _mockNguoiDungService;
        private readonly Mock<IDiemDanhService> _mockDiemDanhService;
        private readonly Mock<IBaoCaoService> _mockBaoCaoService;
        private readonly Mock<IAuthService> _mockAuthService;
        private readonly Mock<IUserSessionService> _mockUserSessionService;

        public TrainerControllerTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            
            // Setup mock services
            _mockLopHocService = new Mock<ILopHocService>();
            _mockBangLuongService = new Mock<IBangLuongService>();
            _mockNguoiDungService = new Mock<INguoiDungService>();
            _mockDiemDanhService = new Mock<IDiemDanhService>();
            _mockBaoCaoService = new Mock<IBaoCaoService>();
            _mockAuthService = new Mock<IAuthService>();
            _mockUserSessionService = new Mock<IUserSessionService>();
            
            var mockLogger = new Mock<ILogger<TrainerController>>();
            
            _controller = new TrainerController(
                _mockLopHocService.Object,
                _mockBangLuongService.Object,
                _mockNguoiDungService.Object,
                _mockDiemDanhService.Object,
                _mockBaoCaoService.Object,
                _mockAuthService.Object,
                _mockUserSessionService.Object,
                mockLogger.Object
            );
            
            SetupMockServices();
        }

        [Fact]
        public async Task Dashboard_WithValidTrainer_ShouldReturnViewWithData()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.Dashboard();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["Trainer"].Should().NotBeNull();
            viewResult.ViewData["MyClasses"].Should().NotBeNull();
        }

        [Fact]
        public async Task Dashboard_WithInvalidUser_ShouldRedirectToIndex()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupInvalidUser();

            // Act
            var result = await _controller.Dashboard();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
        }

        [Fact]
        public async Task MyClasses_WithValidTrainer_ShouldReturnTrainerClasses()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.MyClasses();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().NotBeNull();
        }

        [Fact]
        public async Task MyClasses_WithUnauthorizedUser_ShouldRedirectToHome()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupCustomerUser();

            // Act
            var result = await _controller.MyClasses();

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Index");
            redirectResult.ControllerName.Should().Be("Home");
        }

        [Fact]
        public async Task Schedule_WithValidTrainer_ShouldReturnView()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.Schedule();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["MyClasses"].Should().NotBeNull();
        }

        [Fact]
        public async Task GetScheduleEvents_WithValidTrainer_ShouldReturnJsonResult()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(7);
            
            // Act
            var result = await _controller.GetScheduleEvents(start, end);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            jsonResult!.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetScheduleEvents_WithInvalidDateRange_ShouldReturnError()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(100); // Quá 90 ngày
            
            // Act
            var result = await _controller.GetScheduleEvents(start, end);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(false);
        }

        [Fact]
        public async Task GetScheduleEvents_WithUnauthorizedClassId_ShouldReturnError()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id"); // Trainer 1
            
            var start = DateTime.Today;
            var end = DateTime.Today.AddDays(7);
            var unauthorizedClassId = 3; // Class 3 belongs to Trainer 2
            
            // Act
            var result = await _controller.GetScheduleEvents(start, end, unauthorizedClassId);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(false);
        }

        [Fact]
        public async Task Students_WithValidTrainer_ShouldReturnView()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.Students();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.ViewData["MyClasses"].Should().NotBeNull();
        }

        [Fact]
        public async Task GetAllStudentsByTrainer_WithValidTrainer_ShouldReturnStudents()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.GetAllStudentsByTrainer();
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(true);
            jsonDoc.RootElement.GetProperty("data").ValueKind.Should().NotBe(JsonValueKind.Null);
        }

        [Fact]
        public async Task GetStudentsByClass_WithValidClass_ShouldReturnStudents()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var classId = 1; // Class 1 belongs to Trainer 1
            
            // Act
            var result = await _controller.GetStudentsByClass(classId);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(true);
        }

        [Fact]
        public async Task GetStudentsByClass_WithUnauthorizedClass_ShouldReturnError()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id"); // Trainer 1
            
            var unauthorizedClassId = 3; // Class 3 belongs to Trainer 2
            
            // Act
            var result = await _controller.GetStudentsByClass(unauthorizedClassId);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(false);
        }

        [Fact]
        public async Task GetStudentsByClass_WithInvalidClassId_ShouldReturnError()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var invalidClassId = 0;
            
            // Act
            var result = await _controller.GetStudentsByClass(invalidClassId);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(false);
        }

        [Fact]
        public async Task Salary_WithValidTrainer_ShouldReturnView()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            // Act
            var result = await _controller.Salary();
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            var viewResult = result as ViewResult;
            viewResult!.Model.Should().NotBeNull();
        }

        [Fact]
        public async Task GetSalaryDetails_WithValidMonth_ShouldReturnSalaryData()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var month = DateTime.Now.ToString("yyyy-MM");
            
            // Act
            var result = await _controller.GetSalaryDetails(month);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(true);
        }

        [Fact]
        public async Task GetSalaryDetails_WithInvalidMonthFormat_ShouldReturnError()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            SetupTrainerUser(1, "trainer-1-test-id");
            
            var invalidMonth = "invalid-month";
            
            // Act
            var result = await _controller.GetSalaryDetails(invalidMonth);
            
            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            using var jsonDoc = ParseJsonResult(jsonResult!);
            jsonDoc.RootElement.GetProperty("success").GetBoolean().Should().Be(false);
        }

        private void SetupMockServices()
        {
            // Setup mock services to return data from In-Memory context
            _mockAuthService.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.TaiKhoans.FirstOrDefault(t => t.Id == id));

            _mockNguoiDungService.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) =>
                {
                    var nguoiDung = _context.NguoiDungs.FirstOrDefault(n => n.NguoiDungId == id);
                    return nguoiDung != null ? new NguoiDungDto
                    {
                        NguoiDungId = nguoiDung.NguoiDungId,
                        Ho = nguoiDung.Ho,
                        Ten = nguoiDung.Ten,
                        Email = nguoiDung.Email,
                        SoDienThoai = nguoiDung.SoDienThoai,
                        LoaiNguoiDung = nguoiDung.LoaiNguoiDung,
                        TrangThai = nguoiDung.TrangThai
                    } : null;
                });
            
            _mockLopHocService.Setup(x => x.GetClassesByTrainerAsync(It.IsAny<int>()))
                .ReturnsAsync((int trainerId) => _context.LopHocs.Where(l => l.HlvId == trainerId).ToList());
            
            _mockLopHocService.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.LopHocs.FirstOrDefault(l => l.LopHocId == id));
            
            _mockBangLuongService.Setup(x => x.GetByTrainerIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int trainerId) => _context.BangLuongs.Where(b => b.HlvId == trainerId).ToList());
            
            _mockBangLuongService.Setup(x => x.GetByTrainerAndMonthAsync(It.IsAny<int>(), It.IsAny<string>()))
                .ReturnsAsync((int trainerId, string month) => 
                    _context.BangLuongs.FirstOrDefault(b => b.HlvId == trainerId && b.Thang == month));
        }

        private void SetupTrainerUser(int nguoiDungId, string taiKhoanId)
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(true);
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(false);
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns(nguoiDungId);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns(taiKhoanId);
            _mockUserSessionService.Setup(x => x.GetUserName()).Returns($"trainer{nguoiDungId}@test.com");
        }

        private JsonDocument ParseJsonResult(JsonResult jsonResult)
        {
            var json = JsonSerializer.Serialize(jsonResult.Value);
            return JsonDocument.Parse(json);
        }

        private void SetupInvalidUser()
        {
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns((string?)null);
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns((int?)null);
            _mockUserSessionService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync((UserSessionInfo?)null);
            _mockUserSessionService.Setup(x => x.IsInRole(It.IsAny<string>())).Returns(false);
            _mockUserSessionService.Setup(x => x.GetUserName()).Returns((string?)null);
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
