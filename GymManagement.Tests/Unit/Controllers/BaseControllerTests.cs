using FluentAssertions;
using GymManagement.Web.Controllers;
using GymManagement.Web.Services;
using GymManagement.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymManagement.Tests.Unit.Controllers
{
    /// <summary>
    /// Unit Tests cho BaseController helpers
    /// HOÀN TOÀN AN TOÀN - SỬ DỤNG MOCK SERVICES
    /// </summary>
    public class BaseControllerTests
    {
        /// <summary>
        /// Test Controller kế thừa BaseController để test các helper methods
        /// </summary>
        public class TestController : BaseController
        {
            public TestController(IUserSessionService userSessionService, ILogger<TestController> logger)
                : base(userSessionService, logger)
            {
            }

            // Expose protected methods for testing
            public new bool ValidateTrainerClassAccess(int classId, int? trainerHlvId)
                => base.ValidateTrainerClassAccess(classId, trainerHlvId);

            public new async Task<bool> ValidateTrainerStudentAccessAsync(int studentId, Func<int, Task<bool>> checkStudentInTrainerClasses)
                => await base.ValidateTrainerStudentAccessAsync(studentId, checkStudentInTrainerClasses);

            public new bool ValidateTrainerSalaryAccess(int salaryTrainerId)
                => base.ValidateTrainerSalaryAccess(salaryTrainerId);

            public new void LogUserAction(string action, object? data = null)
                => base.LogUserAction(action, data);

            public new string? GetCurrentUserIdSafe()
                => base.GetCurrentUserIdSafe();

            public new int? GetCurrentNguoiDungIdSafe()
                => base.GetCurrentNguoiDungIdSafe();

            public new bool IsInRoleSafe(string role)
                => base.IsInRoleSafe(role);

            public new IActionResult HandleUserNotFound(string action)
                => base.HandleUserNotFound(action);

            public new IActionResult HandleUnauthorized(string message)
                => base.HandleUnauthorized(message);

            public new IActionResult HandleError(Exception ex, string userMessage)
                => base.HandleError(ex, userMessage);
        }

        private readonly Mock<IUserSessionService> _mockUserSessionService;
        private readonly Mock<ILogger<TestController>> _mockLogger;
        private readonly TestController _controller;

        public BaseControllerTests()
        {
            _mockUserSessionService = new Mock<IUserSessionService>();
            _mockLogger = new Mock<ILogger<TestController>>();
            _controller = new TestController(_mockUserSessionService.Object, _mockLogger.Object);
        }

        [Fact]
        public void ValidateTrainerClassAccess_WithMatchingTrainerId_ShouldReturnTrue()
        {
            // Arrange
            var classId = 1;
            var trainerId = 123;
            
            SetupMockTrainer(trainerId);
            
            // Act
            var result = _controller.ValidateTrainerClassAccess(classId, trainerId);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateTrainerClassAccess_WithDifferentTrainerId_ShouldReturnFalse()
        {
            // Arrange
            var classId = 1;
            var currentTrainerId = 123;
            var classTrainerId = 456;
            
            SetupMockTrainer(currentTrainerId);
            
            // Act
            var result = _controller.ValidateTrainerClassAccess(classId, classTrainerId);
            
            // Assert
            result.Should().BeFalse();
            VerifyLogging(Times.AtLeastOnce()); // Should log unauthorized access
        }

        [Fact]
        public void ValidateTrainerClassAccess_WithNonTrainerRole_ShouldReturnFalse()
        {
            // Arrange
            var classId = 1;
            var trainerId = 123;
            
            SetupMockCustomer();
            
            // Act
            var result = _controller.ValidateTrainerClassAccess(classId, trainerId);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void ValidateTrainerClassAccess_WithNullTrainerId_ShouldReturnFalse()
        {
            // Arrange
            var classId = 1;
            
            SetupMockTrainer(123);
            
            // Act
            var result = _controller.ValidateTrainerClassAccess(classId, null);
            
            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task ValidateTrainerStudentAccessAsync_WithStudentInTrainerClasses_ShouldReturnTrue()
        {
            // Arrange
            var studentId = 101;
            var trainerId = 123;
            
            SetupMockTrainer(trainerId);
            
            // Mock function that checks if student is in trainer's classes
            Task<bool> CheckStudentInTrainerClasses(int tId) => Task.FromResult(tId == trainerId);
            
            // Act
            var result = await _controller.ValidateTrainerStudentAccessAsync(studentId, CheckStudentInTrainerClasses);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task ValidateTrainerStudentAccessAsync_WithStudentNotInTrainerClasses_ShouldReturnFalse()
        {
            // Arrange
            var studentId = 101;
            var trainerId = 123;
            
            SetupMockTrainer(trainerId);
            
            // Mock function that returns false (student not in trainer's classes)
            Task<bool> CheckStudentInTrainerClasses(int tId) => Task.FromResult(false);
            
            // Act
            var result = await _controller.ValidateTrainerStudentAccessAsync(studentId, CheckStudentInTrainerClasses);
            
            // Assert
            result.Should().BeFalse();
            VerifyLogging(Times.AtLeastOnce()); // Should log unauthorized access
        }

        [Fact]
        public void ValidateTrainerSalaryAccess_WithOwnSalary_ShouldReturnTrue()
        {
            // Arrange
            var trainerId = 123;
            
            SetupMockTrainer(trainerId);
            
            // Act
            var result = _controller.ValidateTrainerSalaryAccess(trainerId);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void ValidateTrainerSalaryAccess_WithOtherTrainerSalary_ShouldReturnFalse()
        {
            // Arrange
            var currentTrainerId = 123;
            var otherTrainerId = 456;
            
            SetupMockTrainer(currentTrainerId);
            
            // Act
            var result = _controller.ValidateTrainerSalaryAccess(otherTrainerId);
            
            // Assert
            result.Should().BeFalse();
            VerifyLogging(Times.AtLeastOnce()); // Should log unauthorized access
        }

        [Fact]
        public void ValidateTrainerSalaryAccess_WithAdminRole_ShouldReturnTrue()
        {
            // Arrange
            var trainerId = 123;
            
            SetupMockAdmin();
            
            // Act
            var result = _controller.ValidateTrainerSalaryAccess(trainerId);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void GetCurrentUserIdSafe_WithValidUser_ShouldReturnUserId()
        {
            // Arrange
            var expectedUserId = "test-user-id";
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns(expectedUserId);
            
            // Act
            var result = _controller.GetCurrentUserIdSafe();
            
            // Assert
            result.Should().Be(expectedUserId);
        }

        [Fact]
        public void GetCurrentNguoiDungIdSafe_WithValidUser_ShouldReturnNguoiDungId()
        {
            // Arrange
            var expectedNguoiDungId = 123;
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns(expectedNguoiDungId);
            
            // Act
            var result = _controller.GetCurrentNguoiDungIdSafe();
            
            // Assert
            result.Should().Be(expectedNguoiDungId);
        }

        [Fact]
        public void IsInRoleSafe_WithValidRole_ShouldReturnTrue()
        {
            // Arrange
            var role = "Trainer";
            _mockUserSessionService.Setup(x => x.IsInRole(role)).Returns(true);
            
            // Act
            var result = _controller.IsInRoleSafe(role);
            
            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void HandleUserNotFound_ShouldReturnRedirectResult()
        {
            // Arrange
            var action = "TestAction";
            
            // Act
            var result = _controller.HandleUserNotFound(action);
            
            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            var redirectResult = result as RedirectToActionResult;
            redirectResult!.ActionName.Should().Be("Login");
            redirectResult.ControllerName.Should().Be("Auth");
        }

        [Fact]
        public void HandleUnauthorized_ShouldReturnForbidResult()
        {
            // Arrange
            var message = "Test unauthorized message";
            
            // Act
            var result = _controller.HandleUnauthorized(message);
            
            // Assert
            result.Should().BeOfType<ForbidResult>();
        }

        [Fact]
        public void HandleError_ShouldReturnViewResult()
        {
            // Arrange
            var exception = new Exception("Test exception");
            var userMessage = "Test user message";
            
            // Act
            var result = _controller.HandleError(exception, userMessage);
            
            // Assert
            result.Should().BeOfType<ViewResult>();
            VerifyLogging(Times.AtLeastOnce()); // Should log the error
        }

        [Fact]
        public void LogUserAction_ShouldCallUserSessionService()
        {
            // Arrange
            var action = "TestAction";
            var data = new { TestProperty = "TestValue" };
            
            _mockUserSessionService.Setup(x => x.GetUserName()).Returns("test-user");
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns("test-user-id");
            
            // Act
            _controller.LogUserAction(action, data);
            
            // Assert
            _mockUserSessionService.Verify(x => x.GetUserName(), Times.Once);
            _mockUserSessionService.Verify(x => x.GetUserId(), Times.Once);
            VerifyLogging(Times.Once());
        }

        private void SetupMockTrainer(int nguoiDungId)
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(true);
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(false);
            _mockUserSessionService.Setup(x => x.GetNguoiDungId()).Returns(nguoiDungId);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns($"trainer-{nguoiDungId}-test-id");
        }

        private void SetupMockAdmin()
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(true);
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(false);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns("admin-test-id");
        }

        private void SetupMockCustomer()
        {
            _mockUserSessionService.Setup(x => x.IsInRole("Trainer")).Returns(false);
            _mockUserSessionService.Setup(x => x.IsInRole("Admin")).Returns(false);
            _mockUserSessionService.Setup(x => x.IsInRole("Customer")).Returns(true);
            _mockUserSessionService.Setup(x => x.GetUserId()).Returns("customer-test-id");
        }

        private void VerifyLogging(Times times)
        {
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                times);
        }
    }
}
