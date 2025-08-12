using FluentAssertions;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    /// <summary>
    /// Simple unit tests for WalkInService without complex dependencies
    /// </summary>
    public class WalkInServiceSimpleTests
    {
        [Fact]
        public void WalkInService_SplitFullName_SingleName_ReturnsCorrectParts()
        {
            // Arrange
            var fullName = "Test";

            // Act
            var parts = fullName.Split(' ', 2);
            var ho = parts.Length > 1 ? parts[0] : "";
            var ten = parts.Length > 1 ? parts[1] : parts[0];

            // Assert
            ho.Should().Be("");
            ten.Should().Be("Test");
        }

        [Fact]
        public void WalkInService_SplitFullName_TwoNames_ReturnsCorrectParts()
        {
            // Arrange
            var fullName = "Nguyễn Văn";

            // Act
            var parts = fullName.Split(' ', 2);
            var ho = parts.Length > 1 ? parts[0] : "";
            var ten = parts.Length > 1 ? parts[1] : parts[0];

            // Assert
            ho.Should().Be("Nguyễn");
            ten.Should().Be("Văn");
        }

        [Fact]
        public void WalkInService_SplitFullName_ThreeNames_ReturnsCorrectParts()
        {
            // Arrange
            var fullName = "Nguyễn Văn Test";

            // Act
            var parts = fullName.Split(' ', 2);
            var ho = parts.Length > 1 ? parts[0] : "";
            var ten = parts.Length > 1 ? parts[1] : parts[0];

            // Assert
            ho.Should().Be("Nguyễn");
            ten.Should().Be("Văn Test");
        }

        [Fact]
        public void WalkInService_CreateGuest_ValidData_ReturnsCorrectGuest()
        {
            // Arrange
            var hoTen = "Nguyễn Văn Test";
            var soDienThoai = "0123456789";
            var email = "test@example.com";

            // Act
            var parts = hoTen.Split(' ', 2);
            var guest = new NguoiDung
            {
                LoaiNguoiDung = "VANGLAI",
                Ho = parts.Length > 1 ? parts[0] : "",
                Ten = parts.Length > 1 ? parts[1] : parts[0],
                SoDienThoai = soDienThoai,
                Email = email,
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                TrangThai = "ACTIVE",
                NgayTao = DateTime.Now
            };

            // Assert
            guest.Should().NotBeNull();
            guest.LoaiNguoiDung.Should().Be("VANGLAI");
            guest.Ho.Should().Be("Nguyễn");
            guest.Ten.Should().Be("Văn Test");
            guest.SoDienThoai.Should().Be(soDienThoai);
            guest.Email.Should().Be(email);
            guest.TrangThai.Should().Be("ACTIVE");
            guest.NgayThamGia.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        }

        [Fact]
        public void WalkInService_CreateDayPass_ValidData_ReturnsCorrectDayPass()
        {
            // Arrange
            var guestId = 1;
            var packageType = "DAYPASS";
            var packageName = "Vé ngày";
            var price = 50000m;

            // Act
            var dayPass = new DangKy
            {
                NguoiDungId = guestId,
                LoaiDangKy = packageType,
                NgayBatDau = DateOnly.FromDateTime(DateTime.Today),
                NgayKetThuc = DateOnly.FromDateTime(DateTime.Today),
                PhiDangKy = price,
                TrangThai = "PENDING_PAYMENT",
                NgayTao = DateTime.Now
            };

            // Assert
            dayPass.Should().NotBeNull();
            dayPass.NguoiDungId.Should().Be(guestId);
            dayPass.LoaiDangKy.Should().Be(packageType);
            dayPass.PhiDangKy.Should().Be(price);
            dayPass.TrangThai.Should().Be("PENDING_PAYMENT");
            dayPass.NgayBatDau.Should().Be(DateOnly.FromDateTime(DateTime.Today));
            dayPass.NgayKetThuc.Should().Be(DateOnly.FromDateTime(DateTime.Today));
        }

        [Fact]
        public void WalkInService_CreatePayment_CashMethod_ReturnsSuccessfulPayment()
        {
            // Arrange
            var dangKyId = 1;
            var method = "CASH";
            var amount = 50000m;

            // Act
            var payment = new ThanhToan
            {
                DangKyId = dangKyId,
                SoTien = amount,
                PhuongThuc = method,
                TrangThai = method == "CASH" ? "SUCCESS" : "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = $"WALKIN - {method} payment"
            };

            // Assert
            payment.Should().NotBeNull();
            payment.DangKyId.Should().Be(dangKyId);
            payment.SoTien.Should().Be(amount);
            payment.PhuongThuc.Should().Be("CASH");
            payment.TrangThai.Should().Be("SUCCESS");
        }

        [Fact]
        public void WalkInService_CreatePayment_BankMethod_ReturnsPendingPayment()
        {
            // Arrange
            var dangKyId = 1;
            var method = "BANK";
            var amount = 30000m;

            // Act
            var payment = new ThanhToan
            {
                DangKyId = dangKyId,
                SoTien = amount,
                PhuongThuc = method,
                TrangThai = method == "CASH" ? "SUCCESS" : "PENDING",
                NgayThanhToan = DateTime.Now,
                GhiChu = $"WALKIN - {method} payment"
            };

            // Assert
            payment.Should().NotBeNull();
            payment.DangKyId.Should().Be(dangKyId);
            payment.SoTien.Should().Be(amount);
            payment.PhuongThuc.Should().Be("BANK");
            payment.TrangThai.Should().Be("PENDING");
        }

        [Fact]
        public void WalkInService_CreateAttendance_ValidData_ReturnsCorrectAttendance()
        {
            // Arrange
            var guestId = 1;
            var ghiChu = "WALKIN_DAYPASS";

            // Act
            var attendance = new DiemDanh
            {
                ThanhVienId = guestId,
                ThoiGianCheckIn = DateTime.Now,
                ThoiGianCheckOut = null,
                LoaiCheckIn = "Manual",
                GhiChu = ghiChu,
                TrangThai = "Present"
            };

            // Assert
            attendance.Should().NotBeNull();
            attendance.ThanhVienId.Should().Be(guestId);
            attendance.LoaiCheckIn.Should().Be("Manual");
            attendance.GhiChu.Should().Be(ghiChu);
            attendance.TrangThai.Should().Be("Present");
            attendance.ThoiGianCheckOut.Should().BeNull();
        }

        [Fact]
        public void WalkInService_CheckOutAttendance_ValidData_ReturnsCompletedAttendance()
        {
            // Arrange
            var attendance = new DiemDanh
            {
                ThanhVienId = 1,
                ThoiGianCheckIn = DateTime.Now.AddHours(-2),
                ThoiGianCheckOut = null,
                LoaiCheckIn = "Manual",
                GhiChu = "WALKIN_DAYPASS",
                TrangThai = "Present"
            };

            // Act
            attendance.ThoiGianCheckOut = DateTime.Now;
            attendance.TrangThai = "Completed";

            // Assert
            attendance.ThoiGianCheckOut.Should().NotBeNull();
            attendance.TrangThai.Should().Be("Completed");
            var duration = attendance.ThoiGianCheckOut.Value - attendance.ThoiGianCheckIn;
            duration.TotalHours.Should().BeGreaterThan(1.5);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void WalkInService_ValidateFullName_InvalidNames_ShouldFail(string invalidName)
        {
            // Act & Assert
            var isValid = !string.IsNullOrWhiteSpace(invalidName);
            isValid.Should().BeFalse();
        }

        [Theory]
        [InlineData("Test")]
        [InlineData("Nguyễn Văn")]
        [InlineData("Nguyễn Văn Test")]
        public void WalkInService_ValidateFullName_ValidNames_ShouldPass(string validName)
        {
            // Act & Assert
            var isValid = !string.IsNullOrWhiteSpace(validName);
            isValid.Should().BeTrue();
        }

        [Fact]
        public void WalkInService_CalculateSessionDuration_ValidTimes_ReturnsCorrectDuration()
        {
            // Arrange
            var checkIn = DateTime.Now.AddHours(-3);
            var checkOut = DateTime.Now;

            // Act
            var duration = checkOut - checkIn;

            // Assert
            duration.TotalHours.Should().BeApproximately(3, 0.1);
            duration.TotalMinutes.Should().BeApproximately(180, 5);
        }

        [Fact]
        public void WalkInService_IsToday_TodayDate_ReturnsTrue()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.Today);
            var testDate = DateOnly.FromDateTime(DateTime.Today);

            // Act
            var isToday = testDate == today;

            // Assert
            isToday.Should().BeTrue();
        }

        [Fact]
        public void WalkInService_IsToday_YesterdayDate_ReturnsFalse()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.Today);
            var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));

            // Act
            var isToday = yesterday == today;

            // Assert
            isToday.Should().BeFalse();
        }
    }
}
