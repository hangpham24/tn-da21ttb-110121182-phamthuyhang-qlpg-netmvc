using Moq;
using Xunit;
using GymManagement.Web.Services;
using GymManagement.Web.Data.Repositories;
using GymManagement.Web.Data.Models;
using GymManagement.Web.Models.DTOs;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using GymManagement.Web.Data;
using Microsoft.Extensions.Logging;

namespace GymManagement.Tests.Unit.Services
{
    public class NguoiDungServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock;
        private readonly Mock<INguoiDungRepository> _nguoiDungRepositoryMock;
        private readonly Mock<ILogger<NguoiDungService>> _loggerMock;
        private readonly Mock<IPasswordService> _passwordServiceMock;
        private readonly NguoiDungService _nguoiDungService;

        public NguoiDungServiceTests()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _nguoiDungRepositoryMock = new Mock<INguoiDungRepository>();
            _loggerMock = new Mock<ILogger<NguoiDungService>>();
            _passwordServiceMock = new Mock<IPasswordService>();

            _unitOfWorkMock.Setup(u => u.NguoiDungs).Returns(_nguoiDungRepositoryMock.Object);

            _nguoiDungService = new NguoiDungService(_unitOfWorkMock.Object, _loggerMock.Object, _passwordServiceMock.Object);
        }

        [Fact]
        public async Task GetByIdAsync_ExistingId_ReturnsUser()
        {
            // Arrange
            var expectedUser = new NguoiDung { NguoiDungId = 1, Ho = "Nguyen", Ten = "Van A" };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(expectedUser);

            // Act
            var result = await _nguoiDungService.GetByIdAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedUser.NguoiDungId, result.NguoiDungId);
            _nguoiDungRepositoryMock.Verify(repo => repo.GetByIdAsync(1), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_NonExistingId_ReturnsNull()
        {
            // Arrange
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((NguoiDung)null);

            // Act
            var result = await _nguoiDungService.GetByIdAsync(999);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsAllUsers()
        {
            // Arrange
            var users = new List<NguoiDung>
            {
                new NguoiDung { NguoiDungId = 1, Ho = "Nguyen", Ten = "Van A" },
                new NguoiDung { NguoiDungId = 2, Ho = "Tran", Ten = "Thi B" }
            };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetAllAsync()).ReturnsAsync(users);

            // Act
            var result = await _nguoiDungService.GetAllAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _nguoiDungRepositoryMock.Verify(repo => repo.GetAllAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ExistingId_ReturnsTrue()
        {
            // Arrange
            var existingUser = new NguoiDung { NguoiDungId = 1 };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingUser);
            _nguoiDungRepositoryMock.Setup(repo => repo.DeleteAsync(It.IsAny<NguoiDung>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _nguoiDungService.DeleteAsync(1);

            // Assert
            Assert.True(result);
            _nguoiDungRepositoryMock.Verify(repo => repo.DeleteAsync(existingUser), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_NonExistingId_ReturnsFalse()
        {
            // Arrange
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(It.IsAny<int>())).ReturnsAsync((NguoiDung)null);

            // Act
            var result = await _nguoiDungService.DeleteAsync(999);

            // Assert
            Assert.False(result);
            _nguoiDungRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<NguoiDung>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ValidData_ReturnsCreatedUser()
        {
            // Arrange
            var createDto = new CreateNguoiDungDto
            {
                LoaiNguoiDung = "HVL",
                Ho = "Nguyen",
                Ten = "B",
                Email = "nguyenb@example.com",
                SoDienThoai = "0123456789"
            };

            _nguoiDungRepositoryMock.Setup(repo => repo.AddAsync(It.IsAny<NguoiDung>())).ReturnsAsync(new NguoiDung { NguoiDungId = 1, Ho = "Nguyen", Ten = "B" });
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _nguoiDungService.CreateAsync(createDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(createDto.Ho, result.Ho);
            Assert.Equal(createDto.Ten, result.Ten);
            _nguoiDungRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<NguoiDung>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var updateDto = new UpdateNguoiDungDto
            {
                NguoiDungId = 1,
                Ho = "Tran",
                Ten = "C",
                Email = "tranc@example.com",
                SoDienThoai = "0987654321"
            };
            var existingUser = new NguoiDung { NguoiDungId = 1 };

            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(updateDto.NguoiDungId)).ReturnsAsync(existingUser);
            _nguoiDungRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<NguoiDung>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _nguoiDungService.UpdateAsync(updateDto);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updateDto.Ho, result.Ho);
            Assert.Equal(updateDto.Ten, result.Ten);
            _nguoiDungRepositoryMock.Verify(repo => repo.UpdateAsync(existingUser), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task DeactivateUserAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var existingUser = new NguoiDung { NguoiDungId = 1, TrangThai = "ACTIVE" };

            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingUser);
            _nguoiDungRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<NguoiDung>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _nguoiDungService.DeactivateUserAsync(1);

            // Assert
            Assert.True(result);
            _nguoiDungRepositoryMock.Verify(repo => repo.UpdateAsync(existingUser), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            Assert.Equal("INACTIVE", existingUser.TrangThai);
        }

        [Fact]
        public async Task ActivateUserAsync_ExistingUser_ReturnsTrue()
        {
            // Arrange
            var existingUser = new NguoiDung { NguoiDungId = 1, TrangThai = "INACTIVE" };

            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingUser);
            _nguoiDungRepositoryMock.Setup(repo => repo.UpdateAsync(It.IsAny<NguoiDung>())).Returns(Task.CompletedTask);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _nguoiDungService.ActivateUserAsync(1);

            // Assert
            Assert.True(result);
            _nguoiDungRepositoryMock.Verify(repo => repo.UpdateAsync(existingUser), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
            Assert.Equal("ACTIVE", existingUser.TrangThai);
        }

        [Fact]
        public async Task ChangePasswordAsync_ValidData_ReturnsTrue()
        {
            // Arrange
            var salt = "testsalt";
            var hashedOldPassword = "hashedoldpassword";
            var hashedNewPassword = "hashednewpassword";
            var existingUser = new NguoiDung
            {
                NguoiDungId = 1,
                TaiKhoan = new TaiKhoan
                {
                    MatKhauHash = hashedOldPassword,
                    Salt = salt
                }
            };

            _nguoiDungRepositoryMock.Setup(repo => repo.GetWithTaiKhoanAsync(1)).ReturnsAsync(existingUser);
            _passwordServiceMock.Setup(ps => ps.VerifyPassword("oldpassword", salt, hashedOldPassword)).Returns(true);
            _passwordServiceMock.Setup(ps => ps.HashPassword("newpassword", salt)).Returns(hashedNewPassword);
            _unitOfWorkMock.Setup(uow => uow.SaveChangesAsync()).ReturnsAsync(1);

            // Act
            var result = await _nguoiDungService.ChangePasswordAsync(1, "oldpassword", "newpassword");

            // Assert
            Assert.True(result);
            Assert.Equal(hashedNewPassword, existingUser.TaiKhoan.MatKhauHash);
            _passwordServiceMock.Verify(ps => ps.VerifyPassword("oldpassword", salt, hashedOldPassword), Times.Once);
            _passwordServiceMock.Verify(ps => ps.HashPassword("newpassword", salt), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ChangePasswordAsync_InvalidCurrentPassword_ReturnsFalse()
        {
            // Arrange
            var salt = "testsalt";
            var hashedOldPassword = "hashedoldpassword";
            var existingUser = new NguoiDung
            {
                NguoiDungId = 1,
                TaiKhoan = new TaiKhoan
                {
                    MatKhauHash = hashedOldPassword,
                    Salt = salt
                }
            };

            _nguoiDungRepositoryMock.Setup(repo => repo.GetWithTaiKhoanAsync(1)).ReturnsAsync(existingUser);
            _passwordServiceMock.Setup(ps => ps.VerifyPassword("wrongpassword", salt, hashedOldPassword)).Returns(false);

            // Act
            var result = await _nguoiDungService.ChangePasswordAsync(1, "wrongpassword", "newpassword");

            // Assert
            Assert.False(result);
            _passwordServiceMock.Verify(ps => ps.VerifyPassword("wrongpassword", salt, hashedOldPassword), Times.Once);
            _passwordServiceMock.Verify(ps => ps.HashPassword(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(), Times.Never);
        }

        [Fact]
        public async Task CanDeleteUserAsync_UserHasNoDependencies_ReturnsTrue()
        {
            // Arrange
            var existingUser = new NguoiDung { NguoiDungId = 1, DangKys = new List<DangKy>(), Bookings = new List<Booking>() };
            _nguoiDungRepositoryMock.Setup(repo => repo.GetByIdAsync(1)).ReturnsAsync(existingUser);

            // Act
            var (canDelete, message) = await _nguoiDungService.CanDeleteUserAsync(1);

            // Assert
            Assert.True(canDelete);
            Assert.Equal("Người dùng có thể xóa được.", message);
        }
    }
}
