using FluentAssertions;
using GymManagement.Web.Controllers;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using GymManagement.Tests.TestUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace GymManagement.Tests.Unit.Controllers
{
    /// <summary>
    /// Tests for Check-in/Check-out functionality in ReceptionController
    /// </summary>
    public class ReceptionControllerCheckInTests
    {
        private readonly Mock<IFaceRecognitionService> _mockFaceRecognitionService;
        private readonly Mock<IDiemDanhService> _mockDiemDanhService;
        private readonly Mock<INguoiDungService> _mockNguoiDungService;
        private readonly Mock<IDangKyService> _mockDangKyService;
        private readonly Mock<IThanhToanService> _mockThanhToanService;
        private readonly Mock<ILopHocService> _mockLopHocService;
        private readonly Mock<IWalkInService> _mockWalkInService;
        private readonly Mock<ILogger<ReceptionController>> _mockLogger;
        private readonly ReceptionController _controller;

        public ReceptionControllerCheckInTests()
        {
            _mockFaceRecognitionService = new Mock<IFaceRecognitionService>();
            _mockDiemDanhService = new Mock<IDiemDanhService>();
            _mockNguoiDungService = new Mock<INguoiDungService>();
            _mockDangKyService = new Mock<IDangKyService>();
            _mockThanhToanService = new Mock<IThanhToanService>();
            _mockLopHocService = new Mock<ILopHocService>();
            _mockWalkInService = new Mock<IWalkInService>();
            _mockLogger = new Mock<ILogger<ReceptionController>>();

            _controller = new ReceptionController(
                _mockFaceRecognitionService.Object,
                _mockDiemDanhService.Object,
                _mockNguoiDungService.Object,
                _mockDangKyService.Object,
                _mockThanhToanService.Object,
                _mockLopHocService.Object,
                _mockWalkInService.Object,
                _mockLogger.Object
            );

            // Setup controller context for authorization
            SetupControllerContext();
        }

        private void SetupControllerContext()
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, "admin@test.com"),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = principal
                }
            };
        }

        #region Face Recognition Check-in Tests

        [Fact]
        public async Task AutoCheckIn_ValidFaceDescriptor_SuccessfulCheckIn_ReturnsSuccess()
        {
            // Arrange
            var request = new FaceCheckInRequest
            {
                Descriptor = new double[128] // Valid 128-dimension descriptor
            };

            var member = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Nguyễn",
                Ten = "Văn A",
                TrangThai = "ACTIVE"
            };

            var recognitionResult = CheckInTestHelper.CreateSampleFaceRecognitionResult(true, 1, 0.95);

            _mockFaceRecognitionService.Setup(x => x.RecognizeFaceAsync(It.IsAny<float[]>()))
                .ReturnsAsync(recognitionResult);
            _mockNguoiDungService.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new GymManagement.Web.Models.DTOs.NguoiDungDto { NguoiDungId = 1, Ho = "Nguyễn", Ten = "Văn A", TrangThai = "ACTIVE" });
            _mockDiemDanhService.Setup(x => x.GetLatestAttendanceAsync(1))
                .ReturnsAsync((DiemDanh)null!); // No previous attendance
            _mockDiemDanhService.Setup(x => x.CheckInAsync(1, null))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.AutoCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(true);
            responseType.GetProperty("action").GetValue(response).Should().Be("checkin");
            responseType.GetProperty("memberName").GetValue(response).Should().Be("Nguyễn Văn A");
        }

        [Fact]
        public async Task AutoCheckIn_ValidFaceDescriptor_SuccessfulCheckOut_ReturnsSuccess()
        {
            // Arrange
            var request = new FaceCheckInRequest
            {
                Descriptor = new double[128]
            };

            var member = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Nguyễn",
                Ten = "Văn A",
                TrangThai = "ACTIVE"
            };

            var recognitionResult = CheckInTestHelper.CreateSampleFaceRecognitionResult(true, 1, 0.95);
            var currentSession = CheckInTestHelper.CreateSampleAttendance(1, DateTime.Now.AddHours(-2));
            currentSession.DiemDanhId = 1;

            _mockFaceRecognitionService.Setup(x => x.RecognizeFaceAsync(It.IsAny<float[]>()))
                .ReturnsAsync(recognitionResult);
            _mockNguoiDungService.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new GymManagement.Web.Models.DTOs.NguoiDungDto { NguoiDungId = 1, Ho = "Nguyễn", Ten = "Văn A", TrangThai = "ACTIVE" });
            _mockDiemDanhService.Setup(x => x.GetLatestAttendanceAsync(1))
                .ReturnsAsync(currentSession);

            // Mock for checkout flow - GetByIdAsync and UpdateAsync
            _mockDiemDanhService.Setup(x => x.GetByIdAsync(currentSession.DiemDanhId))
                .ReturnsAsync(currentSession);
            _mockDiemDanhService.Setup(x => x.UpdateAsync(It.IsAny<DiemDanh>()))
                .ReturnsAsync(It.IsAny<DiemDanh>());

            // Act
            var result = await _controller.AutoCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(true);
            responseType.GetProperty("action").GetValue(response).Should().Be("checkout");
        }

        [Fact]
        public async Task AutoCheckIn_InvalidDescriptorLength_ReturnsBadRequest()
        {
            // Arrange
            var request = new FaceCheckInRequest
            {
                Descriptor = new double[64] // Invalid length
            };

            // Act
            var result = await _controller.AutoCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            responseType.GetProperty("message").GetValue(response).Should().Be("Dữ liệu khuôn mặt không hợp lệ");
        }

        [Fact]
        public async Task AutoCheckIn_FaceRecognitionFailed_ReturnsFailure()
        {
            // Arrange
            var request = new FaceCheckInRequest
            {
                Descriptor = new double[128]
            };

            var recognitionResult = CheckInTestHelper.CreateSampleFaceRecognitionResult(false, null, 0.3);

            _mockFaceRecognitionService.Setup(x => x.RecognizeFaceAsync(It.IsAny<float[]>()))
                .ReturnsAsync(recognitionResult);

            // Act
            var result = await _controller.AutoCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            var message = responseType.GetProperty("message").GetValue(response)?.ToString();
            message.Should().NotBeNull();
            message!.Should().Contain("Không nhận diện được khuôn mặt");
        }

        [Fact]
        public async Task AutoCheckIn_MemberNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new FaceCheckInRequest
            {
                Descriptor = new double[128]
            };

            var recognitionResult = CheckInTestHelper.CreateSampleFaceRecognitionResult(true, 1, 0.95);

            _mockFaceRecognitionService.Setup(x => x.RecognizeFaceAsync(It.IsAny<float[]>()))
                .ReturnsAsync(recognitionResult);
            _mockNguoiDungService.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync((GymManagement.Web.Models.DTOs.NguoiDungDto?)null);

            // Act
            var result = await _controller.AutoCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            responseType.GetProperty("message").GetValue(response).Should().Be("Không tìm thấy thông tin hội viên");
        }

        #endregion

        #region Manual Check-in Tests

        [Fact]
        public async Task ManualCheckIn_ValidMember_ReturnsSuccess()
        {
            // Arrange
            var request = new ManualCheckInRequest
            {
                MemberId = 1,
                Note = "Manual check-in by staff"
            };

            var member = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Nguyễn",
                Ten = "Văn A",
                TrangThai = "ACTIVE"
            };

            _mockNguoiDungService.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new GymManagement.Web.Models.DTOs.NguoiDungDto { NguoiDungId = 1, Ho = "Nguyễn", Ten = "Văn A", TrangThai = "ACTIVE" });
            _mockDiemDanhService.Setup(x => x.HasCheckedInTodayAsync(1))
                .ReturnsAsync(false);
            _mockDiemDanhService.Setup(x => x.CheckInAsync(1, "Manual check-in by staff"))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ManualCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(true);
            responseType.GetProperty("memberName").GetValue(response).Should().Be("Nguyễn Văn A");
        }

        [Fact]
        public async Task ManualCheckIn_MemberNotFound_ReturnsFailure()
        {
            // Arrange
            var request = new ManualCheckInRequest
            {
                MemberId = 999
            };

            _mockNguoiDungService.Setup(x => x.GetByIdAsync(999))
                .ReturnsAsync((GymManagement.Web.Models.DTOs.NguoiDungDto?)null);

            // Act
            var result = await _controller.ManualCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            responseType.GetProperty("message").GetValue(response).Should().Be("Không tìm thấy hội viên");
        }

        [Fact]
        public async Task ManualCheckIn_AlreadyCheckedIn_ReturnsFailure()
        {
            // Arrange
            var request = new ManualCheckInRequest
            {
                MemberId = 1
            };

            var member = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Nguyễn",
                Ten = "Văn A"
            };

            _mockNguoiDungService.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new GymManagement.Web.Models.DTOs.NguoiDungDto { NguoiDungId = 1, Ho = "Nguyễn", Ten = "Văn A" });
            _mockDiemDanhService.Setup(x => x.HasCheckedInTodayAsync(1))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ManualCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            responseType.GetProperty("message").GetValue(response).Should().Be("Hội viên đã check-in hôm nay");
        }

        [Fact]
        public async Task ManualCheckIn_CheckInServiceFails_ReturnsFailure()
        {
            // Arrange
            var request = new ManualCheckInRequest
            {
                MemberId = 1
            };

            var member = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Nguyễn",
                Ten = "Văn A"
            };

            _mockNguoiDungService.Setup(x => x.GetByIdAsync(1))
                .ReturnsAsync(new GymManagement.Web.Models.DTOs.NguoiDungDto { NguoiDungId = 1, Ho = "Nguyễn", Ten = "Văn A" });
            _mockDiemDanhService.Setup(x => x.HasCheckedInTodayAsync(1))
                .ReturnsAsync(false);
            _mockDiemDanhService.Setup(x => x.CheckInAsync(1, null))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ManualCheckIn(request);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            responseType.GetProperty("message").GetValue(response).Should().Be("Không thể check-in. Vui lòng thử lại.");
        }

        [Fact]
        public async Task ManualCheckIn_NullRequest_ReturnsFailure()
        {
            // Act
            var result = await _controller.ManualCheckIn(null);

            // Assert
            result.Should().BeOfType<JsonResult>();
            var jsonResult = result as JsonResult;
            var response = jsonResult.Value;

            var responseType = response.GetType();
            responseType.GetProperty("success").GetValue(response).Should().Be(false);
            responseType.GetProperty("message").GetValue(response).Should().Be("Vui lòng chọn hội viên");
        }

        #endregion
    }
}
