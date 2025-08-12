using GymManagement.Web.Authorization;
using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Xunit;

namespace GymManagement.Tests.Unit.Authorization
{
    public class BookingAuthorizationTests
    {
        private readonly BookingAuthorizationHandler _handler;

        public BookingAuthorizationTests()
        {
            _handler = new BookingAuthorizationHandler();
        }

        [Fact]
        public async Task HandleRequirementAsync_AdminUser_CanPerformAllOperations()
        {
            // Arrange
            var user = CreateUser("admin-id", "Admin");
            var booking = CreateBooking("member-id");
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.Cancel }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.Cancel, booking);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_MemberOwnsBooking_CanCancel()
        {
            // Arrange
            var userId = "member-id";
            var user = CreateUser(userId, "Member");
            var booking = CreateBooking(userId);
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.Cancel }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.Cancel, booking);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_MemberDoesNotOwnBooking_CannotCancel()
        {
            // Arrange
            var user = CreateUser("member-1", "Member");
            var booking = CreateBooking("member-2"); // Different member's booking
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.Cancel }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.Cancel, booking);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_TrainerUser_CanViewAllBookings()
        {
            // Arrange
            var user = CreateUser("trainer-id", "Trainer");
            var booking = CreateBooking("member-id");
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.ViewAll }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.ViewAll, booking);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_MemberUser_CannotViewAllBookings()
        {
            // Arrange
            var user = CreateUser("member-id", "Member");
            var booking = CreateBooking("member-id");
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.ViewAll }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.ViewAll, booking);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_MemberUser_CanCreateOwnBooking()
        {
            // Arrange
            var userId = "member-id";
            var user = CreateUser(userId, "Member");
            var booking = CreateBooking(userId);
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.Create }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.Create, booking);

            // Assert
            Assert.True(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_MemberUser_CannotCreateBookingForOthers()
        {
            // Arrange
            var user = CreateUser("member-1", "Member");
            var booking = CreateBooking("member-2"); // Different member's booking
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.Create }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.Create, booking);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task HandleRequirementAsync_NoUserId_Fails()
        {
            // Arrange
            var user = new ClaimsPrincipal(); // No claims
            var booking = CreateBooking("member-id");
            var context = new AuthorizationHandlerContext(
                new[] { BookingOperations.Read }, user, booking);

            // Act
            await _handler.HandleRequirementAsync(context, BookingOperations.Read, booking);

            // Assert
            Assert.False(context.HasSucceeded);
        }

        private static ClaimsPrincipal CreateUser(string userId, string role)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        }

        private static Booking CreateBooking(string ownerUserId)
        {
            return new Booking
            {
                BookingId = 1,
                ThanhVienId = 1,
                ThanhVien = new NguoiDung
                {
                    NguoiDungId = 1,
                    Ho = "Test",
                    Ten = "User",
                    LoaiNguoiDung = "THANHVIEN",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                    TaiKhoan = new TaiKhoan
                    {
                        Id = ownerUserId,
                        TenDangNhap = "testuser",
                        Email = "test@example.com",
                        MatKhauHash = "dummy-hash",
                        Salt = "dummy-salt"
                    }
                },
                LopHocId = 1,
                Ngay = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TrangThai = "BOOKED",
                NgayDat = DateTime.UtcNow
            };
        }
    }
}
