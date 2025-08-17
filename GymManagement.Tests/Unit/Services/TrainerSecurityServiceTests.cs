using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Services;
using GymManagement.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    /// <summary>
    /// Unit Tests cho TrainerSecurityService
    /// HOÀN TOÀN AN TOÀN - SỬ DỤNG IN-MEMORY DATABASE
    /// </summary>
    public class TrainerSecurityServiceTests : IDisposable
    {
        private readonly GymDbContext _context;
        private readonly Mock<ILogger<TrainerSecurityService>> _mockLogger;
        private readonly TrainerSecurityService _service;

        public TrainerSecurityServiceTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<TrainerSecurityService>>();
            
            // Setup services với In-Memory context
            var mockLopHocService = new Mock<ILopHocService>();
            var mockDiemDanhService = new Mock<IDiemDanhService>();
            var mockBangLuongService = new Mock<IBangLuongService>();
            var mockAuthService = new Mock<IAuthService>();
            
            // Setup mock services để sử dụng In-Memory data
            SetupMockServices(mockLopHocService, mockDiemDanhService, mockBangLuongService, mockAuthService);
            
            _service = new TrainerSecurityService(
                mockLopHocService.Object,
                mockDiemDanhService.Object,
                mockBangLuongService.Object,
                mockAuthService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task ValidateTrainerClassAccessAsync_WithValidTrainer_ShouldReturnTrue()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerClassAccessAsync(1, trainer1); // Class 1 belongs to Trainer 1
            
            // Assert
            result.Should().BeTrue();
            VerifySecurityLogging(Times.Never()); // Should not log security events for valid access
        }

        [Fact]
        public async Task ValidateTrainerClassAccessAsync_WithInvalidTrainer_ShouldReturnFalse()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerClassAccessAsync(3, trainer1); // Class 3 belongs to Trainer 2
            
            // Assert
            result.Should().BeFalse();
            VerifySecurityLogging(Times.AtLeastOnce()); // Should log unauthorized access attempt
        }

        [Fact]
        public async Task ValidateTrainerClassAccessAsync_WithNonTrainerRole_ShouldReturnFalse()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var customer = MockUserFactory.CreateMockCustomer("customer-test-id", "customer@test.com");
            MockUserFactory.ValidateMockUser(customer);
            
            // Act
            var result = await _service.ValidateTrainerClassAccessAsync(1, customer);
            
            // Assert
            result.Should().BeFalse();
            VerifySecurityLogging(Times.AtLeastOnce()); // Should log unauthorized role access
        }

        [Fact]
        public async Task ValidateTrainerClassAccessAsync_WithNonExistentClass_ShouldReturnFalse()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerClassAccessAsync(999, trainer1); // Non-existent class
            
            // Assert
            result.Should().BeFalse();
            VerifySecurityLogging(Times.AtLeastOnce()); // Should log class not found
        }

        [Fact]
        public async Task ValidateTrainerStudentAccessAsync_WithValidStudentInTrainerClass_ShouldReturnTrue()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerStudentAccessAsync(101, trainer1); // Student 101 is in Trainer 1's class
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTrainerStudentAccessAsync_WithStudentNotInTrainerClass_ShouldReturnFalse()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerStudentAccessAsync(104, trainer1); // Student 104 is in Trainer 2's class
            
            // Assert
            result.Should().BeFalse();
            VerifySecurityLogging(Times.AtLeastOnce()); // Should log unauthorized student access
        }

        [Fact]
        public async Task ValidateTrainerSalaryAccessAsync_WithOwnSalary_ShouldReturnTrue()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerSalaryAccessAsync(1, trainer1); // Trainer 1's own salary
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTrainerSalaryAccessAsync_WithOtherTrainerSalary_ShouldReturnFalse()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.ValidateTrainerSalaryAccessAsync(2, trainer1); // Trainer 2's salary
            
            // Assert
            result.Should().BeFalse();
            VerifySecurityLogging(Times.AtLeastOnce()); // Should log unauthorized salary access
        }

        [Fact]
        public async Task ValidateTrainerSalaryAccessAsync_WithAdminRole_ShouldReturnTrue()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var admin = MockUserFactory.CreateMockAdmin("admin-test-id", "admin@test.com");
            MockUserFactory.ValidateMockUser(admin);
            
            // Act
            var result = await _service.ValidateTrainerSalaryAccessAsync(1, admin); // Admin can access any salary
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void LogSecurityEvent_ShouldLogWithCorrectFormat()
        {
            // Arrange
            var trainer = MockUserFactory.CreateMockTrainer("trainer-test-id", "trainer@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer);
            var eventData = new { ClassId = 1, Action = "Test" };
            
            // Act
            _service.LogSecurityEvent("TEST_EVENT", trainer, eventData);
            
            // Assert
            VerifySecurityLogging(Times.Once());
        }

        [Fact]
        public async Task GetTrainerClassesSecureAsync_WithValidTrainer_ShouldReturnTrainerClasses()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act
            var result = await _service.GetTrainerClassesSecureAsync(trainer1);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2); // Trainer 1 has 2 classes
        }

        [Fact]
        public async Task GetTrainerClassesSecureAsync_WithNonTrainer_ShouldReturnEmptyList()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var customer = MockUserFactory.CreateMockCustomer("customer-test-id", "customer@test.com");
            MockUserFactory.ValidateMockUser(customer);
            
            // Act
            var result = await _service.GetTrainerClassesSecureAsync(customer);
            
            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            VerifySecurityLogging(Times.AtLeastOnce()); // Should log unauthorized access
        }

        [Theory]
        [InlineData("")]
        [InlineData(null)]
        public async Task ValidateTrainerClassAccessAsync_WithInvalidUserId_ShouldReturnFalse(string? userId)
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var invalidUser = MockUserFactory.CreateMockTrainer(userId ?? "", "trainer@test.com", 1);
            
            // Act & Assert
            var result = await _service.ValidateTrainerClassAccessAsync(1, invalidUser);
            result.Should().BeFalse();
        }

        private void SetupMockServices(
            Mock<ILopHocService> mockLopHocService,
            Mock<IDiemDanhService> mockDiemDanhService,
            Mock<IBangLuongService> mockBangLuongService,
            Mock<IAuthService> mockAuthService)
        {
            // Setup mock services to return data from In-Memory context
            // This ensures tests use the seeded test data
            
            mockAuthService.Setup(x => x.GetUserByIdAsync("trainer-1-test-id"))
                .ReturnsAsync(() => _context.TaiKhoans.FirstOrDefault(t => t.Id == "trainer-1-test-id"));

            mockAuthService.Setup(x => x.GetUserByIdAsync("trainer-2-test-id"))
                .ReturnsAsync(() => _context.TaiKhoans.FirstOrDefault(t => t.Id == "trainer-2-test-id"));
            
            mockLopHocService.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.LopHocs.FirstOrDefault(l => l.LopHocId == id));
            
            mockLopHocService.Setup(x => x.GetClassesByTrainerAsync(It.IsAny<int>()))
                .ReturnsAsync((int trainerId) => _context.LopHocs.Where(l => l.HlvId == trainerId).ToList());
        }

        private void VerifySecurityLogging(Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("SECURITY EVENT")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }

        public void Dispose()
        {
            _context.Dispose();
        }
    }
}
