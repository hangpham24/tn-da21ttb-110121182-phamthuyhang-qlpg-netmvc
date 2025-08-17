using GymManagement.Web;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Claims;
using System.Text;
using Xunit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Encodings.Web;

namespace GymManagement.Tests.Integration
{
    public class BookingAuthorizationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public BookingAuthorizationIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Cancel_MemberCancelsOwnBooking_ReturnsSuccess()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with in-memory for testing
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GymDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<GymDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                    // Add fake authentication
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            }).CreateClient();

            // Seed test data
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            
            var taiKhoan = new TaiKhoan
            {
                Id = "member-1",
                TenDangNhap = "member1",
                Email = "member1@test.com",
                MatKhauHash = "dummy-hash",
                Salt = "dummy-salt"
            };

            var nguoiDung = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Test",
                Ten = "Member",
                Email = "member1@test.com",
                LoaiNguoiDung = "THANHVIEN",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today)
            };

            var lopHoc = new LopHoc
            {
                LopHocId = 1,
                TenLop = "Test Class",
                SucChua = 10,
                GioBatDau = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                GioKetThuc = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                ThuTrongTuan = "2,4,6"
            };

            var booking = new Booking
            {
                BookingId = 1,
                ThanhVienId = 1,
                LopHocId = 1,
                Ngay = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TrangThai = "BOOKED",
                NgayDat = DateTime.UtcNow
            };

            context.TaiKhoans.Add(taiKhoan);
            context.NguoiDungs.Add(nguoiDung);
            context.LopHocs.Add(lopHoc);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Set authentication header for member-1
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "member-1:Member");

            // Act
            var response = await client.PostAsync("/Booking/Cancel", 
                new StringContent($"id=1", Encoding.UTF8, "application/x-www-form-urlencoded"));

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("success", content);
        }

        [Fact]
        public async Task Cancel_MemberTriesToCancelOthersBooking_ReturnsForbidden()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with in-memory for testing
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GymDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<GymDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                    // Add fake authentication
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            }).CreateClient();

            // Seed test data - booking belongs to member-2, but member-1 tries to cancel
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<GymDbContext>();
            
            var taiKhoan1 = new TaiKhoan
            {
                Id = "member-1",
                TenDangNhap = "member1",
                Email = "member1@test.com",
                MatKhauHash = "dummy-hash",
                Salt = "dummy-salt"
            };

            var taiKhoan2 = new TaiKhoan
            {
                Id = "member-2",
                TenDangNhap = "member2",
                Email = "member2@test.com",
                MatKhauHash = "dummy-hash",
                Salt = "dummy-salt"
            };

            var nguoiDung1 = new NguoiDung
            {
                NguoiDungId = 1,
                Ho = "Test",
                Ten = "Member1",
                Email = "member1@test.com",
                LoaiNguoiDung = "THANHVIEN",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today)
            };

            var nguoiDung2 = new NguoiDung
            {
                NguoiDungId = 2,
                Ho = "Test",
                Ten = "Member2",
                Email = "member2@test.com",
                LoaiNguoiDung = "THANHVIEN",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today)
            };

            var lopHoc = new LopHoc
            {
                LopHocId = 1,
                TenLop = "Test Class",
                SucChua = 10,
                GioBatDau = TimeOnly.FromTimeSpan(TimeSpan.FromHours(9)),
                GioKetThuc = TimeOnly.FromTimeSpan(TimeSpan.FromHours(10)),
                ThuTrongTuan = "2,4,6"
            };

            var booking = new Booking
            {
                BookingId = 1,
                ThanhVienId = 2, // Belongs to member-2
                LopHocId = 1,
                Ngay = DateOnly.FromDateTime(DateTime.Today.AddDays(1)),
                TrangThai = "BOOKED",
                NgayDat = DateTime.UtcNow
            };

            context.TaiKhoans.AddRange(taiKhoan1, taiKhoan2);
            context.NguoiDungs.AddRange(nguoiDung1, nguoiDung2);
            context.LopHocs.Add(lopHoc);
            context.Bookings.Add(booking);
            await context.SaveChangesAsync();

            // Set authentication header for member-1 (trying to cancel member-2's booking)
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "member-1:Member");

            // Act
            var response = await client.PostAsync("/Booking/Cancel", 
                new StringContent($"id=1", Encoding.UTF8, "application/x-www-form-urlencoded"));

            // Assert
            var content = await response.Content.ReadAsStringAsync();
            Assert.Contains("không có quyền", content.ToLower());
        }

        [Fact]
        public async Task Index_AdminUser_CanViewAllBookings()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with in-memory for testing
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GymDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<GymDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                    // Add fake authentication
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            }).CreateClient();

            // Set authentication header for admin
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "admin-1:Admin");

            // Act
            var response = await client.GetAsync("/Booking");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Index_MemberUser_GetsForbidden()
        {
            // Arrange
            var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Replace database with in-memory for testing
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<GymDbContext>));
                    if (descriptor != null)
                        services.Remove(descriptor);

                    services.AddDbContext<GymDbContext>(options =>
                        options.UseInMemoryDatabase("TestDb_" + Guid.NewGuid()));

                    // Add fake authentication
                    services.AddAuthentication("Test")
                        .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                            "Test", options => { });
                });
            }).CreateClient();

            // Set authentication header for member
            client.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Test", "member-1:Member");

            // Act
            var response = await client.GetAsync("/Booking");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder, ISystemClock clock)
            : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Test "))
            {
                return Task.FromResult(AuthenticateResult.Fail("No auth header"));
            }

            var authValue = authHeader.Substring("Test ".Length);
            var parts = authValue.Split(':');
            if (parts.Length != 2)
            {
                return Task.FromResult(AuthenticateResult.Fail("Invalid auth header"));
            }

            var userId = parts[0];
            var role = parts[1];

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
