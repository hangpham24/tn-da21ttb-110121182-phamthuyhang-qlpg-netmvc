using FluentAssertions;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Services;
using GymManagement.Web.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GymManagement.Tests.Unit.Services
{
    /// <summary>
    /// Unit tests for BangLuongService
    /// Tests cover basic CRUD operations and business logic
    /// </summary>
    public class BangLuongServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<IBangLuongRepository> _bangLuongRepositoryMock;
        private readonly Mock<INguoiDungRepository> _nguoiDungRepositoryMock;
        private readonly Mock<IThongBaoService> _thongBaoServiceMock;
        private readonly Mock<IEmailService> _emailServiceMock;
        private readonly Mock<IMemoryCache> _cacheMock;
        private readonly Mock<IAuditLogService> _auditLogMock;
        private readonly Mock<ILogger<BangLuongService>> _loggerMock;
        private readonly IOptions<CommissionConfiguration> _commissionConfig;
        private readonly BangLuongService _bangLuongService;

        public BangLuongServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _bangLuongRepositoryMock = new Mock<IBangLuongRepository>();
            _nguoiDungRepositoryMock = new Mock<INguoiDungRepository>();
            _thongBaoServiceMock = new Mock<IThongBaoService>();
            _emailServiceMock = new Mock<IEmailService>();
            _cacheMock = new Mock<IMemoryCache>();
            _auditLogMock = new Mock<IAuditLogService>();
            _loggerMock = new Mock<ILogger<BangLuongService>>();

            // Setup commission configuration
            var commissionConfig = new CommissionConfiguration
            {
                PackageCommissionRate = 0.05m,
                ClassCommissionRate = 0.03m,
                PersonalTrainingRate = 0.10m,
                PerformanceBonusRate = 0.02m,
                AttendanceBonusRate = 0.01m,
                MaxCommissionPerMonth = 5000000m,
                MinStudentCountForBonus = 10,
                MinAttendanceRateForBonus = 0.8m,
                CommissionTiers = new List<CommissionTier>
                {
                    new CommissionTier { MinRevenue = 0, MaxRevenue = 10000000, Rate = 0.05m },
                    new CommissionTier { MinRevenue = 10000000, MaxRevenue = 20000000, Rate = 0.07m },
                    new CommissionTier { MinRevenue = 20000000, MaxRevenue = decimal.MaxValue, Rate = 0.10m }
                }
            };
            _commissionConfig = Options.Create(commissionConfig);

            _bangLuongService = new BangLuongService(
                _unitOfWorkMock.Object,
                _bangLuongRepositoryMock.Object,
                _nguoiDungRepositoryMock.Object,
                _thongBaoServiceMock.Object,
                _emailServiceMock.Object,
                _cacheMock.Object,
                _auditLogMock.Object,
                _commissionConfig,
                _loggerMock.Object
            );
        }

        #region Basic CRUD Operations Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllSalaries()
        {
            // Arrange
            var expectedSalaries = new List<BangLuong>
            {
                new BangLuong { BangLuongId = 1, HlvId = 1, Thang = "2024-01", LuongCoBan = 10000000m, TienHoaHong = 500000m },
                new BangLuong { BangLuongId = 2, HlvId = 2, Thang = "2024-01", LuongCoBan = 12000000m, TienHoaHong = 800000m }
            };

            _bangLuongRepositoryMock.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(expectedSalaries);

            // Act
            var result = await _bangLuongService.GetAllAsync();

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedSalaries);
            _bangLuongRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ShouldReturnSalary()
        {
            // Arrange
            var expectedSalary = new BangLuong 
            { 
                BangLuongId = 1, 
                HlvId = 1, 
                Thang = "2024-01",
                LuongCoBan = 10000000m,
                TienHoaHong = 500000m
            };
            
            _bangLuongRepositoryMock.Setup(repo => repo.GetByIdAsync(1))
                .ReturnsAsync(expectedSalary);

            // Act
            var result = await _bangLuongService.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedSalary);
            _bangLuongRepositoryMock.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ShouldReturnNull()
        {
            // Arrange
            _bangLuongRepositoryMock.Setup(repo => repo.GetByIdAsync(999))
                .ReturnsAsync((BangLuong?)null);

            // Act
            var result = await _bangLuongService.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
            _bangLuongRepositoryMock.Verify(repo => repo.GetByIdAsync(999), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ValidSalary_ShouldCreateSuccessfully()
        {
            // Arrange
            var salary = new BangLuong 
            { 
                HlvId = 1, 
                Thang = "2024-01",
                LuongCoBan = 10000000m,
                TienHoaHong = 500000m
            };

            var createdSalary = new BangLuong 
            { 
                BangLuongId = 1,
                HlvId = 1, 
                Thang = "2024-01",
                LuongCoBan = 10000000m,
                TienHoaHong = 500000m,
                NgayTao = DateTime.Now
            };

            _bangLuongRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<BangLuong>()))
                .ReturnsAsync(createdSalary);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync())
                .ReturnsAsync(1);

            // Act
            var result = await _bangLuongService.CreateAsync(salary);

            // Assert
            result.Should().NotBeNull();
            result.BangLuongId.Should().Be(1);
            result.HlvId.Should().Be(1);
            result.Thang.Should().Be("2024-01");
            _bangLuongRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<BangLuong>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region Commission Configuration Tests

        [Fact]
        public void CommissionConfiguration_ShouldHaveCorrectDefaultValues()
        {
            // Assert - Verify commission rates are set correctly
            _commissionConfig.Value.PackageCommissionRate.Should().Be(0.05m);
            _commissionConfig.Value.ClassCommissionRate.Should().Be(0.03m);
            _commissionConfig.Value.PersonalTrainingRate.Should().Be(0.10m);
            _commissionConfig.Value.PerformanceBonusRate.Should().Be(0.02m);
            _commissionConfig.Value.AttendanceBonusRate.Should().Be(0.01m);
            _commissionConfig.Value.MaxCommissionPerMonth.Should().Be(5000000m);
            _commissionConfig.Value.MinStudentCountForBonus.Should().Be(10);
            _commissionConfig.Value.MinAttendanceRateForBonus.Should().Be(0.8m);
        }

        [Fact]
        public void CommissionConfiguration_ShouldHaveValidTiers()
        {
            // Assert - Verify commission tiers are configured correctly
            var tiers = _commissionConfig.Value.CommissionTiers;
            tiers.Should().NotBeNull();
            tiers.Should().HaveCount(3);
            
            tiers[0].MinRevenue.Should().Be(0);
            tiers[0].MaxRevenue.Should().Be(10000000);
            tiers[0].Rate.Should().Be(0.05m);
            
            tiers[1].MinRevenue.Should().Be(10000000);
            tiers[1].MaxRevenue.Should().Be(20000000);
            tiers[1].Rate.Should().Be(0.07m);
            
            tiers[2].MinRevenue.Should().Be(20000000);
            tiers[2].MaxRevenue.Should().Be(decimal.MaxValue);
            tiers[2].Rate.Should().Be(0.10m);
        }

        #endregion

        #region Service Initialization Tests

        [Fact]
        public async Task Service_ShouldBeProperlyInitialized()
        {
            // Assert - Verify service is properly initialized with all dependencies
            _bangLuongService.Should().NotBeNull();
            
            // Verify that service can handle basic operations without throwing
            var act = async () => await _bangLuongService.GetAllAsync();
            await act.Should().NotThrowAsync();
        }

        [Fact]
        public async Task CalculateCommissionAsync_ValidTrainerAndMonth_ShouldReturnCommission()
        {
            // Arrange
            var trainerId = 1;
            var month = "2024-01";

            // Act
            var result = await _bangLuongService.CalculateCommissionAsync(trainerId, month);

            // Assert
            result.Should().BeGreaterOrEqualTo(0);
        }

        #endregion
    }
}
