using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using GymManagement.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    public class DiemDanhServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDiemDanhRepository> _diemDanhRepositoryMock;
        private readonly Mock<INguoiDungRepository> _nguoiDungRepositoryMock;
        private readonly Mock<IThongBaoService> _thongBaoServiceMock;
        private readonly DiemDanhService _diemDanhService;

        public DiemDanhServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _diemDanhRepositoryMock = new Mock<IDiemDanhRepository>();
            _nguoiDungRepositoryMock = new Mock<INguoiDungRepository>();
            _thongBaoServiceMock = new Mock<IThongBaoService>();
            
            // Setup context with in-memory database
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new GymDbContext(options);
            _unitOfWorkMock.Setup(uow => uow.Context).Returns(context);
            
            var mockFaceRecognitionService = new Mock<IFaceRecognitionService>();

            _diemDanhService = new DiemDanhService(
                _unitOfWorkMock.Object,
                _diemDanhRepositoryMock.Object,
                _nguoiDungRepositoryMock.Object,
                _thongBaoServiceMock.Object,
                mockFaceRecognitionService.Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllAttendances()
        {
            // Arrange
            var attendances = new List<DiemDanh>
            {
                new DiemDanh { DiemDanhId = 1 },
                new DiemDanh { DiemDanhId = 2 }
            };
            _diemDanhRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(attendances);

            // Act
            var result = await _diemDanhService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task CheckInAsync_ValidMember_ReturnsTrue()
        {
            // Arrange
            var member = new NguoiDung 
            { 
                NguoiDungId = 1, 
                TrangThai = "ACTIVE", 
                LoaiNguoiDung = "THANHVIEN" 
            };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(member);
            _diemDanhRepositoryMock.Setup(repo => repo.HasAttendanceToday(1)).ReturnsAsync(false);

            // Act
            var result = await _diemDanhService.CheckInAsync(1);

            // Assert
            result.Should().BeTrue();
            _diemDanhRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<DiemDanh>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Exactly(2));
        }

        [Fact]
        public async Task CheckInAsync_InactiveMember_ReturnsFalse()
        {
            // Arrange
            var member = new NguoiDung 
            { 
                NguoiDungId = 1, 
                TrangThai = "INACTIVE", 
                LoaiNguoiDung = "THANHVIEN" 
            };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(member);

            // Act
            var result = await _diemDanhService.CheckInAsync(1);

            // Assert
            result.Should().BeFalse();
            _diemDanhRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<DiemDanh>()), Times.Never);
        }

        [Fact]
        public async Task CheckInAsync_AlreadyCheckedIn_ReturnsFalse()
        {
            // Arrange
            var member = new NguoiDung 
            { 
                NguoiDungId = 1, 
                TrangThai = "ACTIVE", 
                LoaiNguoiDung = "THANHVIEN" 
            };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(member);
            _diemDanhRepositoryMock.Setup(repo => repo.HasAttendanceToday(1)).ReturnsAsync(true);

            // Act
            var result = await _diemDanhService.CheckInAsync(1);

            // Assert
            result.Should().BeFalse();
            _diemDanhRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<DiemDanh>()), Times.Never);
        }

        [Fact]
        public async Task GetTodayAttendanceAsync_ReturnsTodayAttendances()
        {
            // Arrange
            var todayAttendances = new List<DiemDanh>
            {
                new DiemDanh { DiemDanhId = 1, ThoiGian = DateTime.Today },
                new DiemDanh { DiemDanhId = 2, ThoiGian = DateTime.Today }
            };
            _diemDanhRepositoryMock.Setup(repo => repo.GetTodayAttendanceAsync())
                .ReturnsAsync(todayAttendances);

            // Act
            var result = await _diemDanhService.GetTodayAttendanceAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task HasCheckedInTodayAsync_ReturnsCorrectValue()
        {
            // Arrange
            _diemDanhRepositoryMock.Setup(repo => repo.HasAttendanceToday(1)).ReturnsAsync(true);

            // Act
            var result = await _diemDanhService.HasCheckedInTodayAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task GetTodayAttendanceCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            _diemDanhRepositoryMock.Setup(repo => repo.CountAttendanceByDateAsync(It.IsAny<DateTime>()))
                .ReturnsAsync(15);

            // Act
            var result = await _diemDanhService.GetTodayAttendanceCountAsync();

            // Assert
            result.Should().Be(15);
        }

        [Fact]
        public async Task GetMemberAttendanceCountAsync_ReturnsCorrectCount()
        {
            // Arrange
            var startDate = DateTime.Today.AddDays(-30);
            var endDate = DateTime.Today;
            _diemDanhRepositoryMock.Setup(repo => repo.CountAttendanceByMemberAsync(1, startDate, endDate))
                .ReturnsAsync(20);

            // Act
            var result = await _diemDanhService.GetMemberAttendanceCountAsync(1, startDate, endDate);

            // Assert
            result.Should().Be(20);
        }

        [Fact]
        public async Task DeleteAsync_ExistingAttendance_ReturnsTrue()
        {
            // Arrange
            var attendance = new DiemDanh { DiemDanhId = 1 };
            _diemDanhRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(attendance);

            // Act
            var result = await _diemDanhService.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            _diemDanhRepositoryMock.Verify(repo => repo.DeleteAsync(attendance), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingAttendance_ReturnsFalse()
        {
            // Arrange
            _diemDanhRepositoryMock.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((DiemDanh?)null);

            // Act
            var result = await _diemDanhService.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task GetFirstAttendanceDateAsync_ReturnsCorrectDate()
        {
            // Arrange
            var attendances = new List<DiemDanh>
            {
                new DiemDanh { ThoiGianCheckIn = DateTime.Today.AddDays(-30) },
                new DiemDanh { ThoiGianCheckIn = DateTime.Today.AddDays(-60) },
                new DiemDanh { ThoiGianCheckIn = DateTime.Today.AddDays(-10) }
            };
            _diemDanhRepositoryMock.Setup(repo => repo.GetByNguoiDungIdAsync(1))
                .ReturnsAsync(attendances);

            // Act
            var result = await _diemDanhService.GetFirstAttendanceDateAsync(1);

            // Assert
            result.Should().Be(DateTime.Today.AddDays(-60));
        }
    }
}
