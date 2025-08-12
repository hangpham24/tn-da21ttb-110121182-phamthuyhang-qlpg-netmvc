using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    public class LopHocServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<ILopHocRepository> _lopHocRepositoryMock;
        private readonly Mock<IBookingRepository> _bookingRepositoryMock;
        private readonly IMemoryCache _cache;
        private readonly LopHocService _lopHocService;

        public LopHocServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _lopHocRepositoryMock = new Mock<ILopHocRepository>();
            _bookingRepositoryMock = new Mock<IBookingRepository>();
            _cache = new MemoryCache(new MemoryCacheOptions());
            
            _lopHocService = new LopHocService(
                _unitOfWorkMock.Object,
                _lopHocRepositoryMock.Object,
                _bookingRepositoryMock.Object,
                _cache);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllClasses()
        {
            // Arrange
            var lopHocs = new List<LopHoc>
            {
                new LopHoc { LopHocId = 1, TenLop = "Yoga cơ bản" },
                new LopHoc { LopHocId = 2, TenLop = "Gym nâng cao" }
            };
            _lopHocRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(lopHocs);

            // Act
            var result = await _lopHocService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().TenLop.Should().Be("Yoga cơ bản");
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsLopHoc()
        {
            // Arrange
            var lopHoc = new LopHoc { LopHocId = 1, TenLop = "Yoga" };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);

            // Act
            var result = await _lopHocService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result?.TenLop.Should().Be("Yoga");
        }

        [Fact]
        public async Task CreateAsync_ValidInput_ReturnsCreatedLopHoc()
        {
            // Arrange
            var lopHoc = new LopHoc
            {
                TenLop = "Lớp mới",
                GioBatDau = new TimeOnly(8, 0),
                GioKetThuc = new TimeOnly(9, 0),
                SucChua = 30,
                ThuTrongTuan = "Thứ 2,Thứ 4"
            };

            _lopHocRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<LopHoc>()))
                .ReturnsAsync((LopHoc lh) => { lh.LopHocId = 1; return lh; });

            // Act
            var result = await _lopHocService.CreateAsync(lopHoc);

            // Assert
            result.Should().NotBeNull();
            result.TenLop.Should().Be("Lớp mới");
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_InvalidTimeRange_ThrowsException()
        {
            // Arrange
            var lopHoc = new LopHoc
            {
                TenLop = "Lớp lỗi",
                GioBatDau = new TimeOnly(9, 0),
                GioKetThuc = new TimeOnly(8, 0), // End before start
                SucChua = 30
            };

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _lopHocService.CreateAsync(lopHoc));
        }

        [Fact]
        public async Task DeleteAsync_ExistingClass_ReturnsTrue()
        {
            // Arrange
            var lopHoc = new LopHoc { LopHocId = 1 };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);

            // Act
            var result = await _lopHocService.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingClass_ReturnsFalse()
        {
            // Arrange
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((LopHoc?)null);

            // Act
            var result = await _lopHocService.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task IsClassAvailableAsync_AvailableClass_ReturnsTrue()
        {
            // Arrange
            var lopHoc = new LopHoc { LopHocId = 1, TrangThai = "OPEN", SucChua = 30 };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);
            _bookingRepositoryMock.Setup(repo => repo.CountBookingsForClassAsync(1, It.IsAny<DateTime>()))
                .ReturnsAsync(10);

            // Act
            var result = await _lopHocService.IsClassAvailableAsync(1, DateTime.Today);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task IsClassAvailableAsync_FullClass_ReturnsFalse()
        {
            // Arrange
            var lopHoc = new LopHoc { LopHocId = 1, TrangThai = "OPEN", SucChua = 30 };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);
            _bookingRepositoryMock.Setup(repo => repo.CountBookingsForClassAsync(1, It.IsAny<DateTime>()))
                .ReturnsAsync(30);

            // Act
            var result = await _lopHocService.IsClassAvailableAsync(1, DateTime.Today);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetAvailableSlotsAsync_ReturnsCorrectSlots()
        {
            // Arrange
            var lopHoc = new LopHoc { LopHocId = 1, SucChua = 30 };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);
            _bookingRepositoryMock.Setup(repo => repo.CountBookingsForClassAsync(1, It.IsAny<DateTime>()))
                .ReturnsAsync(22);

            // Act
            var result = await _lopHocService.GetAvailableSlotsAsync(1, DateTime.Today);

            // Assert
            result.Should().Be(8); // 30 - 22 = 8
        }

        [Fact]
        public async Task CanDeleteClassAsync_NoActiveData_ReturnsCanDelete()
        {
            // Arrange
            var lopHoc = new LopHoc 
            { 
                LopHocId = 1,
                DangKys = new List<DangKy>(),
                Bookings = new List<Booking>(),
                LichLops = new List<LichLop>(),
                BuoiTaps = new List<BuoiTap>()
            };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);

            // Act
            var (canDelete, message) = await _lopHocService.CanDeleteClassAsync(1);

            // Assert
            canDelete.Should().BeTrue();
            message.Should().Be("Lớp học có thể xóa được.");
        }

        [Fact]
        public async Task CanDeleteClassAsync_HasActiveRegistrations_ReturnsCannotDelete()
        {
            // Arrange
            var lopHoc = new LopHoc 
            { 
                LopHocId = 1,
                DangKys = new List<DangKy> 
                { 
                    new DangKy { TrangThai = "ACTIVE" },
                    new DangKy { TrangThai = "ACTIVE" }
                }
            };
            _lopHocRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(lopHoc);

            // Act
            var (canDelete, message) = await _lopHocService.CanDeleteClassAsync(1);

            // Assert
            canDelete.Should().BeFalse();
            message.Should().Contain("2 học viên đang hoạt động");
        }
    }
}
