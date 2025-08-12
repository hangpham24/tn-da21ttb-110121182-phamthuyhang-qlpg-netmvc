using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace GymManagement.Tests.Integration
{
    public class BookingIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BookingIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's ApplicationDbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<GymDbContext>));

                    if (descriptor != null)
                        services.Remove(descriptor);

                    // Add ApplicationDbContext using an in-memory database for testing
                    services.AddDbContext<GymDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });

                    // Add fake authentication for testing
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });
                });
            });

            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task BookClass_ValidRequest_ReturnsSuccess()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            
            await SeedTestDataAsync(context);

            var requestData = new
            {
                classId = 1,
                date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                note = "Integration test booking"
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/Booking/BookClass", content);

            // Assert
            response.Should().NotBeNull();
            var responseContent = await response.Content.ReadAsStringAsync();
            
            // Verify booking was created in database
            var booking = await context.Bookings.FirstOrDefaultAsync();
            booking.Should().NotBeNull();
            booking!.LopHocId.Should().Be(1);
            booking.TrangThai.Should().Be("BOOKED");
        }

        [Fact]
        public async Task BookClass_ClassFull_ReturnsFailure()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            
            await SeedTestDataAsync(context, classCapacity: 1);
            
            // Create existing booking to fill the class
            var existingBooking = new Booking
            {
                ThanhVienId = 2,
                LopHocId = 1,
                Ngay = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                NgayDat = DateOnly.FromDateTime(DateTime.Today),
                TrangThai = "BOOKED"
            };
            context.Bookings.Add(existingBooking);
            await context.SaveChangesAsync();

            var requestData = new
            {
                classId = 1,
                date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                note = "Should fail - class full"
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/Booking/BookClass", content);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("đầy");
            
            // Verify no additional booking was created
            var bookingCount = await context.Bookings.CountAsync();
            bookingCount.Should().Be(1, "Should still have only the original booking");
        }

        [Fact]
        public async Task BookClass_DuplicateBooking_ReturnsFailure()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            
            await SeedTestDataAsync(context);
            
            // Create existing booking for same user and class
            var existingBooking = new Booking
            {
                ThanhVienId = 1,
                LopHocId = 1,
                Ngay = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                NgayDat = DateOnly.FromDateTime(DateTime.Today),
                TrangThai = "BOOKED"
            };
            context.Bookings.Add(existingBooking);
            await context.SaveChangesAsync();

            var requestData = new
            {
                classId = 1,
                date = DateTime.Today.AddDays(1).ToString("yyyy-MM-dd"),
                note = "Should fail - duplicate booking"
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Act
            var response = await _client.PostAsync("/Booking/BookClass", content);

            // Assert
            var responseContent = await response.Content.ReadAsStringAsync();
            responseContent.Should().Contain("đã đặt lịch");
            
            // Verify no additional booking was created
            var bookingCount = await context.Bookings.CountAsync();
            bookingCount.Should().Be(1, "Should still have only the original booking");
        }

        private async Task SeedTestDataAsync(GymDbContext context, int classCapacity = 20)
        {
            // Clear existing data
            context.Bookings.RemoveRange(context.Bookings);
            context.LopHocs.RemoveRange(context.LopHocs);
            context.NguoiDungs.RemoveRange(context.NguoiDungs);
            context.TaiKhoans.RemoveRange(context.TaiKhoans);
            await context.SaveChangesAsync();

            // Seed test data
            var taiKhoan = new TaiKhoan
            {
                Id = "test-user-id",
                TenDangNhap = "testuser",
                Email = "test@example.com",
                MatKhauHash = "dummy-hash",
                Salt = "dummy-salt"
            };

            var nguoiDung = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Test",
                Ten = "User",
                Email = "test@example.com",
                LoaiNguoiDung = "THANHVIEN",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today)
            };

            var lopHoc = new LopHoc
            {
                LopHocId = 1,
                TenLop = "Test Yoga Class",
                SucChua = classCapacity,
                TrangThai = "OPEN",
                GioBatDau = new TimeOnly(7, 0),
                GioKetThuc = new TimeOnly(8, 0),
                ThuTrongTuan = "Monday,Wednesday,Friday"
            };

            context.TaiKhoans.Add(taiKhoan);
            context.NguoiDungs.Add(nguoiDung);
            context.LopHocs.Add(lopHoc);
            await context.SaveChangesAsync();
        }
    }

    // Test Authentication Handler for Integration Tests
    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Role, "Member")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
