using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace GymManagement.Tests.Performance
{
    public class ConcurrentBookingTests
    {
        private readonly ITestOutputHelper _output;
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IThongBaoService> _thongBaoServiceMock;
        private readonly Mock<ILogger<BookingService>> _loggerMock;

        public ConcurrentBookingTests(ITestOutputHelper output)
        {
            _output = output;
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _thongBaoServiceMock = new Mock<IThongBaoService>();
            _loggerMock = new Mock<ILogger<BookingService>>();
        }

        private async Task<GymDbContext> CreateSqliteContextAsync()
        {
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseSqlite($"Data Source=:memory:")
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
                .Options;

            var context = new GymDbContext(options);
            await context.Database.OpenConnectionAsync();
            await context.Database.EnsureCreatedAsync();
            return context;
        }

        [Fact]
        public async Task ConcurrentBookings_10Users1Slot_OnlyOneSucceeds()
        {
            // Arrange
            using var context = await CreateSqliteContextAsync();
            await SeedTestDataAsync(context, classCapacity: 1);

            var bookingService = CreateBookingService(context);
            const int concurrentUsers = 10;
            var bookingDate = DateTime.Today.AddDays(1);

            _output.WriteLine($"🚀 Starting concurrent booking test with {concurrentUsers} users for 1 slot");

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<(bool Success, string ErrorMessage)>>();

            for (int i = 1; i <= concurrentUsers; i++)
            {
                var userId = i;
                var task = bookingService.BookClassWithTransactionAsync(
                    userId, 1, bookingDate, $"Concurrent booking from user {userId}");
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            _output.WriteLine($"⏱️  Total execution time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"✅ Successful bookings: {successCount}");
            _output.WriteLine($"❌ Failed bookings: {failureCount}");

            successCount.Should().Be(1, "Only one booking should succeed");
            failureCount.Should().Be(concurrentUsers - 1, "All other bookings should fail");

            // Verify database consistency
            var actualBookings = await context.Bookings.CountAsync();
            actualBookings.Should().Be(1, "Database should contain exactly one booking");

            // Performance assertion
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "Concurrent bookings should complete within 5 seconds");
        }

        [Fact]
        public async Task ConcurrentBookings_20Users10Slots_Exactly10Succeed()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new GymDbContext(options);
            await SeedTestDataAsync(context, classCapacity: 10);

            var bookingService = CreateBookingService(context);
            const int concurrentUsers = 20;
            const int expectedSuccessful = 10;
            var bookingDate = DateTime.Today.AddDays(1);

            _output.WriteLine($"🚀 Starting concurrent booking test with {concurrentUsers} users for {expectedSuccessful} slots");

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<(bool Success, string ErrorMessage)>>();

            for (int i = 1; i <= concurrentUsers; i++)
            {
                var userId = i;
                var task = bookingService.BookClassWithTransactionAsync(
                    userId, 1, bookingDate, $"Concurrent booking from user {userId}");
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            _output.WriteLine($"⏱️  Total execution time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"✅ Successful bookings: {successCount}");
            _output.WriteLine($"❌ Failed bookings: {failureCount}");

            successCount.Should().Be(expectedSuccessful, $"Exactly {expectedSuccessful} bookings should succeed");
            failureCount.Should().Be(concurrentUsers - expectedSuccessful, "Remaining bookings should fail");

            // Verify database consistency
            var actualBookings = await context.Bookings.CountAsync();
            actualBookings.Should().Be(expectedSuccessful, $"Database should contain exactly {expectedSuccessful} bookings");

            // Performance assertion
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(10000, "Concurrent bookings should complete within 10 seconds");
        }

        [Fact]
        public async Task ConcurrentBookings_SameUser_OnlyOneSucceeds()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new GymDbContext(options);
            await SeedTestDataAsync(context, classCapacity: 20);

            var bookingService = CreateBookingService(context);
            const int concurrentAttempts = 5;
            const int userId = 1;
            var bookingDate = DateTime.Today.AddDays(1);

            _output.WriteLine($"🚀 Testing duplicate booking prevention: {concurrentAttempts} concurrent attempts from same user");

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<(bool Success, string ErrorMessage)>>();

            for (int i = 1; i <= concurrentAttempts; i++)
            {
                var attemptNumber = i;
                var task = bookingService.BookClassWithTransactionAsync(
                    userId, 1, bookingDate, $"Attempt {attemptNumber} from user {userId}");
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            _output.WriteLine($"⏱️  Total execution time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"✅ Successful bookings: {successCount}");
            _output.WriteLine($"❌ Failed bookings: {failureCount}");

            successCount.Should().Be(1, "Only one booking should succeed for the same user");
            failureCount.Should().Be(concurrentAttempts - 1, "All duplicate attempts should fail");

            // Verify database consistency
            var actualBookings = await context.Bookings.Where(b => b.ThanhVienId == userId).CountAsync();
            actualBookings.Should().Be(1, "Database should contain exactly one booking for the user");
        }

        [Theory]
        [InlineData(50, 25)]
        [InlineData(100, 50)]
        public async Task ConcurrentBookings_HighLoad_MaintainsDataIntegrity(int totalUsers, int classCapacity)
        {
            // Arrange
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            using var context = new GymDbContext(options);
            await SeedTestDataAsync(context, classCapacity: classCapacity);

            var bookingService = CreateBookingService(context);
            var bookingDate = DateTime.Today.AddDays(1);

            _output.WriteLine($"🚀 High load test: {totalUsers} users competing for {classCapacity} slots");

            // Act
            var stopwatch = Stopwatch.StartNew();
            var tasks = new List<Task<(bool Success, string ErrorMessage)>>();

            for (int i = 1; i <= totalUsers; i++)
            {
                var userId = i;
                var task = bookingService.BookClassWithTransactionAsync(
                    userId, 1, bookingDate, $"High load booking from user {userId}");
                tasks.Add(task);
            }

            var results = await Task.WhenAll(tasks);
            stopwatch.Stop();

            // Assert
            var successCount = results.Count(r => r.Success);
            var failureCount = results.Count(r => !r.Success);

            _output.WriteLine($"⏱️  Total execution time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"📊 Average time per booking: {(double)stopwatch.ElapsedMilliseconds / totalUsers:F2}ms");
            _output.WriteLine($"✅ Successful bookings: {successCount}");
            _output.WriteLine($"❌ Failed bookings: {failureCount}");

            successCount.Should().Be(classCapacity, $"Exactly {classCapacity} bookings should succeed");
            failureCount.Should().Be(totalUsers - classCapacity, "Remaining bookings should fail due to capacity");

            // Verify database consistency
            var actualBookings = await context.Bookings.CountAsync();
            actualBookings.Should().Be(classCapacity, $"Database should contain exactly {classCapacity} bookings");

            // Performance assertion - should handle high load reasonably
            var avgTimePerBooking = (double)stopwatch.ElapsedMilliseconds / totalUsers;
            avgTimePerBooking.Should().BeLessThan(100, "Average booking time should be under 100ms");
        }

        private BookingService CreateBookingService(GymDbContext context)
        {
            _unitOfWorkMock.Setup(uow => uow.Context).Returns(context);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).Returns(Task.FromResult(0));

            var bookingRepository = new BookingRepository(context);
            var lopHocRepository = new LopHocRepository(context);

            return new BookingService(
                _unitOfWorkMock.Object,
                bookingRepository,
                lopHocRepository,
                _thongBaoServiceMock.Object);
        }

        private async Task SeedTestDataAsync(GymDbContext context, int classCapacity = 20)
        {
            var lopHoc = new LopHoc
            {
                LopHocId = 1,
                TenLop = "Performance Test Class",
                SucChua = classCapacity,
                TrangThai = "OPEN",
                GioBatDau = new TimeOnly(7, 0),
                GioKetThuc = new TimeOnly(8, 0),
                ThuTrongTuan = "Monday,Wednesday,Friday"
            };

            context.LopHocs.Add(lopHoc);

            // Add test users
            for (int i = 1; i <= 100; i++)
            {
                var nguoiDung = new NguoiDung
                {
                    NguoiDungId = i,
                    Ho = "Test",
                    Ten = $"User {i}",
                    Email = $"testuser{i}@example.com",
                    LoaiNguoiDung = "THANHVIEN",
                    NgayThamGia = DateOnly.FromDateTime(DateTime.Today)
                };
                context.NguoiDungs.Add(nguoiDung);
            }

            await context.SaveChangesAsync();
        }
    }
}
