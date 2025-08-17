using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using GymManagement.Tests.TestUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    public class DangKyServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IDangKyRepository> _dangKyRepositoryMock;
        private readonly Mock<IGoiTapRepository> _goiTapRepositoryMock;
        private readonly Mock<ILopHocRepository> _lopHocRepositoryMock;
        private readonly Mock<IThongBaoService> _thongBaoServiceMock;
        private readonly DangKyService _dangKyService;

        public DangKyServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _dangKyRepositoryMock = new Mock<IDangKyRepository>();
            _goiTapRepositoryMock = new Mock<IGoiTapRepository>();
            _lopHocRepositoryMock = new Mock<ILopHocRepository>();
            _thongBaoServiceMock = new Mock<IThongBaoService>();
            
            // Setup context with in-memory database
            var options = new DbContextOptionsBuilder<GymDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new GymDbContext(options);
            _unitOfWorkMock.Setup(uow => uow.Context).Returns(context);
            
            _dangKyService = new DangKyService(
                _unitOfWorkMock.Object,
                _dangKyRepositoryMock.Object,
                _goiTapRepositoryMock.Object,
                _lopHocRepositoryMock.Object,
                _thongBaoServiceMock.Object,
                new Mock<ILogger<DangKyService>>().Object);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllRegistrations()
        {
            // Arrange
            var registrations = new List<DangKy>
            {
                new DangKy { DangKyId = 1 },
                new DangKy { DangKyId = 2 }
            };
            _dangKyRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(registrations);

            // Act
            var result = await _dangKyService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
        }

        [Fact]
        public async Task CreateAsync_ValidRegistration_ReturnsCreatedRegistration()
        {
            // Arrange
            var dangKy = new DangKy { DangKyId = 1, NguoiDungId = 1 };
            _dangKyRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<DangKy>()))
                .ReturnsAsync((DangKy d) => { d.DangKyId = 1; return d; });

            // Act
            var result = await _dangKyService.CreateAsync(dangKy);

            // Assert
            result.Should().NotBeNull();
            result.DangKyId.Should().Be(1);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterPackageAsync_ValidInput_ReturnsTrue()
        {
            // Arrange
            var goiTap = new GoiTap { GoiTapId = 1 };
            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(goiTap);
            _dangKyRepositoryMock.Setup(repo => repo.GetByMemberIdAsync(1)).ReturnsAsync(new List<DangKy>());

            // Act
            var result = await _dangKyService.RegisterPackageAsync(1, 1, 6);

            // Assert
            result.Should().BeTrue();
            _dangKyRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<DangKy>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task RegisterPackageAsync_UserHasActivePackage_ReturnsFalse()
        {
            // Arrange
            var goiTap = new GoiTap { GoiTapId = 1 };
            var existingPackage = new DangKy
            {
                GoiTapId = 1,
                TrangThai = "ACTIVE",
                NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
            };

            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(goiTap);
            _dangKyRepositoryMock.Setup(repo => repo.GetByMemberIdAsync(1)).ReturnsAsync(new List<DangKy> { existingPackage });

            // Act
            var result = await _dangKyService.RegisterPackageAsync(1, 1, 6);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HasActivePackageRegistrationAsync_UserHasActivePackage_ReturnsTrue()
        {
            // Arrange
            var activePackage = new DangKy
            {
                GoiTapId = 1,
                TrangThai = "ACTIVE",
                NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(1))
            };

            _dangKyRepositoryMock.Setup(repo => repo.GetByMemberIdAsync(1))
                .ReturnsAsync(new List<DangKy> { activePackage });

            // Act
            var result = await _dangKyService.HasActivePackageAsync(1);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public async Task DeleteAsync_ExistingRegistration_ReturnsTrue()
        {
            // Arrange
            var registration = new DangKy { DangKyId = 1 };
            _dangKyRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(registration);

            // Act
            var result = await _dangKyService.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
        }
    }
}

