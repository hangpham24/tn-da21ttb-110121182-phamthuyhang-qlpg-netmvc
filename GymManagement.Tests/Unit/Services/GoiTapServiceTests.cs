using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Models.DTOs;
using GymManagement.Web.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    public class GoiTapServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IGoiTapRepository> _goiTapRepositoryMock;
        private readonly Mock<IDangKyRepository> _dangKyRepositoryMock;
        private readonly GoiTapService _goiTapService;

        public GoiTapServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _goiTapRepositoryMock = new Mock<IGoiTapRepository>();
            _dangKyRepositoryMock = new Mock<IDangKyRepository>();
            
            _unitOfWorkMock.Setup(uow => uow.GoiTaps).Returns(_goiTapRepositoryMock.Object);
            _unitOfWorkMock.Setup(uow => uow.DangKys).Returns(_dangKyRepositoryMock.Object);
            
            _goiTapService = new GoiTapService(_unitOfWorkMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsGoiTapDto()
        {
            // Arrange
            var goiTap = new GoiTap 
            { 
                GoiTapId = 1, 
                TenGoi = "Gói VIP", 
                ThoiHanThang = 3, 
                SoBuoiToiDa = 90, 
                Gia = 1500000 
            };
            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(goiTap);

            // Act
            var result = await _goiTapService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result?.GoiTapId.Should().Be(1);
            result?.TenGoi.Should().Be("Gói VIP");
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((GoiTap?)null);

            // Act
            var result = await _goiTapService.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllGoiTapDtos()
        {
            // Arrange
            var goiTaps = new List<GoiTap>
            {
                new GoiTap { GoiTapId = 1, TenGoi = "Gói 1", Gia = 500000 },
                new GoiTap { GoiTapId = 2, TenGoi = "Gói 2", Gia = 1000000 }
            };
            _goiTapRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(goiTaps);

            // Act
            var result = await _goiTapService.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.First().TenGoi.Should().Be("Gói 1");
        }

        [Fact]
        public async Task CreateAsync_ValidInput_ReturnsGoiTapDto()
        {
            // Arrange
            var createDto = new CreateGoiTapDto
            {
                TenGoi = "Gói mới",
                ThoiHanThang = 6,
                SoBuoiToiDa = 180,
                Gia = 2000000,
                MoTa = "Gói tập 6 tháng"
            };

            _goiTapRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<GoiTap>()))
                .ReturnsAsync((GoiTap gt) => { gt.GoiTapId = 1; return gt; });

            // Act
            var result = await _goiTapService.CreateAsync(createDto);

            // Assert
            result.Should().NotBeNull();
            result.TenGoi.Should().Be("Gói mới");
            result.Gia.Should().Be(2000000);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingGoiTap_ReturnsUpdatedGoiTapDto()
        {
            // Arrange
            var existingGoiTap = new GoiTap
            {
                GoiTapId = 1,
                TenGoi = "Gói cũ",
                ThoiHanThang = 1,
                SoBuoiToiDa = 30,
                Gia = 500000
            };

            var updateDto = new UpdateGoiTapDto
            {
                GoiTapId = 1,
                TenGoi = "Gói cập nhật",
                ThoiHanThang = 3,
                SoBuoiToiDa = 90,
                Gia = 1200000,
                MoTa = "Mô tả mới"
            };

            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingGoiTap);

            // Act
            var result = await _goiTapService.UpdateAsync(updateDto);

            // Assert
            result.Should().NotBeNull();
            result.TenGoi.Should().Be("Gói cập nhật");
            result.Gia.Should().Be(1200000);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_NonExistingGoiTap_ThrowsArgumentException()
        {
            // Arrange
            var updateDto = new UpdateGoiTapDto { GoiTapId = 999 };
            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(999)).ReturnsAsync((GoiTap?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _goiTapService.UpdateAsync(updateDto));
        }

        [Fact]
        public async Task DeleteAsync_CanDelete_ReturnsTrue()
        {
            // Arrange
            var goiTap = new GoiTap { GoiTapId = 1 };
            _goiTapRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(goiTap);
            _dangKyRepositoryMock.Setup(repo => repo.ExistsAsync(It.IsAny<Expression<Func<DangKy, bool>>>()))
                .ReturnsAsync(false);

            // Act
            var result = await _goiTapService.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_HasActiveRegistrations_ReturnsFalse()
        {
            // Arrange
            _dangKyRepositoryMock.Setup(repo => repo.ExistsAsync(It.IsAny<Expression<Func<DangKy, bool>>>()))
                .ReturnsAsync(true);

            // Act
            var result = await _goiTapService.DeleteAsync(1);

            // Assert
            result.Should().BeFalse();
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task GetActivePackagesAsync_ReturnsActivePackages()
        {
            // Arrange
            var goiTaps = new List<GoiTap>
            {
                new GoiTap { GoiTapId = 1, TenGoi = "Gói Active 1" },
                new GoiTap { GoiTapId = 2, TenGoi = "Gói Active 2" }
            };
            _goiTapRepositoryMock.Setup(repo => repo.GetActivePackagesAsync()).ReturnsAsync(goiTaps);

            // Act
            var result = await _goiTapService.GetActivePackagesAsync();

            // Assert
            result.Should().HaveCount(2);
            result.All(x => x.TrangThai == "ACTIVE").Should().BeTrue();
        }

        [Fact]
        public async Task GetByPriceRangeAsync_ReturnsPriceRangePackages()
        {
            // Arrange
            var goiTaps = new List<GoiTap>
            {
                new GoiTap { GoiTapId = 1, TenGoi = "Gói 1", Gia = 600000 },
                new GoiTap { GoiTapId = 2, TenGoi = "Gói 2", Gia = 800000 }
            };
            _goiTapRepositoryMock.Setup(repo => repo.GetByPriceRangeAsync(500000, 1000000))
                .ReturnsAsync(goiTaps);

            // Act
            var result = await _goiTapService.GetByPriceRangeAsync(500000, 1000000);

            // Assert
            result.Should().HaveCount(2);
            result.All(x => x.Gia >= 500000 && x.Gia <= 1000000).Should().BeTrue();
        }
    }
}
