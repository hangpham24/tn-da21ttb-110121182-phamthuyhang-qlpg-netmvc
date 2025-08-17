using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Services;
using GymManagement.Tests.TestHelpers;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GymManagement.Tests.Security
{
    /// <summary>
    /// Comprehensive Security Tests cho Trainer role
    /// HOÀN TOÀN AN TOÀN - SỬ DỤNG IN-MEMORY DATABASE VÀ MOCK DATA
    /// </summary>
    public class TrainerSecurityTests : IDisposable
    {
        private readonly GymDbContext _context;
        private readonly TrainerSecurityService _securityService;
        private readonly Mock<ILogger<TrainerSecurityService>> _mockLogger;

        public TrainerSecurityTests()
        {
            _context = TestDbContextFactory.CreateInMemoryContext();
            _mockLogger = new Mock<ILogger<TrainerSecurityService>>();
            
            // Setup security service với mock dependencies
            var mockLopHocService = new Mock<ILopHocService>();
            var mockDiemDanhService = new Mock<IDiemDanhService>();
            var mockBangLuongService = new Mock<IBangLuongService>();
            var mockAuthService = new Mock<IAuthService>();
            
            SetupMockServices(mockLopHocService, mockDiemDanhService, mockBangLuongService, mockAuthService);
            
            _securityService = new TrainerSecurityService(
                mockLopHocService.Object,
                mockDiemDanhService.Object,
                mockBangLuongService.Object,
                mockAuthService.Object,
                _mockLogger.Object
            );
        }

        [Fact]
        public async Task CrossTrainerAccess_TrainerCannotAccessOtherTrainerClasses()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            var trainer2 = MockUserFactory.CreateMockTrainer("trainer-2-test-id", "trainer2@test.com", 2);
            
            MockUserFactory.ValidateMockUser(trainer1);
            MockUserFactory.ValidateMockUser(trainer2);
            
            // Act & Assert
            // Trainer 1 trying to access their own class (should succeed)
            var result1 = await _securityService.ValidateTrainerClassAccessAsync(1, trainer1);
            result1.Should().BeTrue();
            
            // Trainer 1 trying to access Trainer 2's class (should fail)
            var result2 = await _securityService.ValidateTrainerClassAccessAsync(3, trainer1);
            result2.Should().BeFalse();
            
            // Trainer 2 trying to access their own class (should succeed)
            var result3 = await _securityService.ValidateTrainerClassAccessAsync(3, trainer2);
            result3.Should().BeTrue();
            
            // Trainer 2 trying to access Trainer 1's class (should fail)
            var result4 = await _securityService.ValidateTrainerClassAccessAsync(1, trainer2);
            result4.Should().BeFalse();
        }

        [Fact]
        public async Task CrossTrainerAccess_TrainerCannotAccessOtherTrainerStudents()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            var trainer2 = MockUserFactory.CreateMockTrainer("trainer-2-test-id", "trainer2@test.com", 2);
            
            MockUserFactory.ValidateMockUser(trainer1);
            MockUserFactory.ValidateMockUser(trainer2);
            
            // Act & Assert
            // Trainer 1 accessing their own students (should succeed)
            var result1 = await _securityService.ValidateTrainerStudentAccessAsync(101, trainer1); // Student in Trainer 1's class
            result1.Should().BeTrue();
            
            // Trainer 1 trying to access Trainer 2's students (should fail)
            var result2 = await _securityService.ValidateTrainerStudentAccessAsync(104, trainer1); // Student in Trainer 2's class
            result2.Should().BeFalse();
            
            // Trainer 2 accessing their own students (should succeed)
            var result3 = await _securityService.ValidateTrainerStudentAccessAsync(104, trainer2);
            result3.Should().BeTrue();
            
            // Trainer 2 trying to access Trainer 1's students (should fail)
            var result4 = await _securityService.ValidateTrainerStudentAccessAsync(101, trainer2);
            result4.Should().BeFalse();
        }

        [Fact]
        public async Task CrossTrainerAccess_TrainerCannotAccessOtherTrainerSalary()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            var trainer2 = MockUserFactory.CreateMockTrainer("trainer-2-test-id", "trainer2@test.com", 2);
            
            MockUserFactory.ValidateMockUser(trainer1);
            MockUserFactory.ValidateMockUser(trainer2);
            
            // Act & Assert
            // Trainer 1 accessing their own salary (should succeed)
            var result1 = await _securityService.ValidateTrainerSalaryAccessAsync(1, trainer1);
            result1.Should().BeTrue();
            
            // Trainer 1 trying to access Trainer 2's salary (should fail)
            var result2 = await _securityService.ValidateTrainerSalaryAccessAsync(2, trainer1);
            result2.Should().BeFalse();
            
            // Trainer 2 accessing their own salary (should succeed)
            var result3 = await _securityService.ValidateTrainerSalaryAccessAsync(2, trainer2);
            result3.Should().BeTrue();
            
            // Trainer 2 trying to access Trainer 1's salary (should fail)
            var result4 = await _securityService.ValidateTrainerSalaryAccessAsync(1, trainer2);
            result4.Should().BeFalse();
        }

        [Fact]
        public async Task UnauthorizedRoleAccess_CustomerCannotAccessTrainerFunctions()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var customer = MockUserFactory.CreateMockCustomer("customer-test-id", "customer@test.com");
            MockUserFactory.ValidateMockUser(customer);
            
            // Act & Assert
            var classAccessResult = await _securityService.ValidateTrainerClassAccessAsync(1, customer);
            classAccessResult.Should().BeFalse();
            
            var studentAccessResult = await _securityService.ValidateTrainerStudentAccessAsync(101, customer);
            studentAccessResult.Should().BeFalse();
            
            var salaryAccessResult = await _securityService.ValidateTrainerSalaryAccessAsync(1, customer);
            salaryAccessResult.Should().BeFalse();
            
            var classesResult = await _securityService.GetTrainerClassesSecureAsync(customer);
            classesResult.Should().BeEmpty();
        }

        [Fact]
        public async Task UnauthorizedRoleAccess_UnknownRoleCannotAccessTrainerFunctions()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var unauthorizedUser = MockUserFactory.CreateMockUnauthorizedUser("unknown-test-id", "unknown@test.com");
            MockUserFactory.ValidateMockUser(unauthorizedUser);
            
            // Act & Assert
            var classAccessResult = await _securityService.ValidateTrainerClassAccessAsync(1, unauthorizedUser);
            classAccessResult.Should().BeFalse();
            
            var studentAccessResult = await _securityService.ValidateTrainerStudentAccessAsync(101, unauthorizedUser);
            studentAccessResult.Should().BeFalse();
            
            var salaryAccessResult = await _securityService.ValidateTrainerSalaryAccessAsync(1, unauthorizedUser);
            salaryAccessResult.Should().BeFalse();
        }

        [Fact]
        public async Task AdminPrivileges_AdminCanAccessAllTrainerData()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var admin = MockUserFactory.CreateMockAdmin("admin-test-id", "admin@test.com");
            MockUserFactory.ValidateMockUser(admin);
            
            // Act & Assert
            // Admin should be able to access any trainer's salary
            var salary1AccessResult = await _securityService.ValidateTrainerSalaryAccessAsync(1, admin);
            salary1AccessResult.Should().BeTrue();
            
            var salary2AccessResult = await _securityService.ValidateTrainerSalaryAccessAsync(2, admin);
            salary2AccessResult.Should().BeTrue();
        }

        [Fact]
        public async Task SecurityLogging_UnauthorizedAccessAttemptsAreLogged()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            var customer = MockUserFactory.CreateMockCustomer("customer-test-id", "customer@test.com");
            
            MockUserFactory.ValidateMockUser(trainer1);
            MockUserFactory.ValidateMockUser(customer);
            
            // Act
            await _securityService.ValidateTrainerClassAccessAsync(3, trainer1); // Unauthorized class access
            await _securityService.ValidateTrainerClassAccessAsync(1, customer); // Unauthorized role access
            
            // Assert
            VerifySecurityLogging(Times.AtLeast(2));
        }

        [Fact]
        public async Task EdgeCase_NonExistentResourceAccess()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act & Assert
            // Non-existent class
            var classResult = await _securityService.ValidateTrainerClassAccessAsync(999, trainer1);
            classResult.Should().BeFalse();
            
            // Non-existent student
            var studentResult = await _securityService.ValidateTrainerStudentAccessAsync(999, trainer1);
            studentResult.Should().BeFalse();
            
            // Non-existent salary
            var salaryResult = await _securityService.ValidateTrainerSalaryAccessAsync(999, trainer1);
            salaryResult.Should().BeFalse();
        }

        [Fact]
        public async Task EdgeCase_InvalidUserData()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            
            // Create user with invalid/missing claims
            var invalidUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Role, "Trainer")
                // Missing NameIdentifier claim
            }, "TestAuth"));
            
            // Act & Assert
            var classResult = await _securityService.ValidateTrainerClassAccessAsync(1, invalidUser);
            classResult.Should().BeFalse();
            
            var studentResult = await _securityService.ValidateTrainerStudentAccessAsync(101, invalidUser);
            studentResult.Should().BeFalse();
            
            var salaryResult = await _securityService.ValidateTrainerSalaryAccessAsync(1, invalidUser);
            salaryResult.Should().BeFalse();
        }

        [Fact]
        public async Task PerformanceTest_SecurityValidationWithLargeDataset()
        {
            // Arrange
            await TestDataSeeder.SeedCompleteTestDataAsync(_context);
            var trainer1 = MockUserFactory.CreateMockTrainer("trainer-1-test-id", "trainer1@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer1);
            
            // Act - Test multiple security validations
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            
            var tasks = new List<Task<bool>>();
            for (int i = 0; i < 100; i++)
            {
                tasks.Add(_securityService.ValidateTrainerClassAccessAsync(1, trainer1));
                tasks.Add(_securityService.ValidateTrainerStudentAccessAsync(101, trainer1));
                tasks.Add(_securityService.ValidateTrainerSalaryAccessAsync(1, trainer1));
            }
            
            await Task.WhenAll(tasks);
            stopwatch.Stop();
            
            // Assert
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000); // Should complete within 2 seconds
            tasks.All(t => t.Result).Should().BeTrue(); // All valid access should succeed
        }

        [Fact]
        public void SecurityEventLogging_LogsWithCorrectFormat()
        {
            // Arrange
            var trainer = MockUserFactory.CreateMockTrainer("trainer-test-id", "trainer@test.com", 1);
            MockUserFactory.ValidateMockUser(trainer);
            
            var eventData = new { ClassId = 1, Action = "Test", Severity = "High" };
            
            // Act
            _securityService.LogSecurityEvent("TEST_SECURITY_EVENT", trainer, eventData);
            
            // Assert
            VerifySecurityLogging(Times.Once());
        }

        private void SetupMockServices(
            Mock<ILopHocService> mockLopHocService,
            Mock<IDiemDanhService> mockDiemDanhService,
            Mock<IBangLuongService> mockBangLuongService,
            Mock<IAuthService> mockAuthService)
        {
            // Setup mock services to return data from In-Memory context
            mockAuthService.Setup(x => x.GetUserByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((string id) => _context.TaiKhoans.FirstOrDefault(t => t.Id == id));
            
            mockLopHocService.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.LopHocs.FirstOrDefault(l => l.LopHocId == id));
            
            mockLopHocService.Setup(x => x.GetClassesByTrainerAsync(It.IsAny<int>()))
                .ReturnsAsync((int trainerId) => _context.LopHocs.Where(l => l.HlvId == trainerId).ToList());
            
            mockDiemDanhService.Setup(x => x.GetByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((int id) => _context.DiemDanhs.FirstOrDefault(d => d.DiemDanhId == id));
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
