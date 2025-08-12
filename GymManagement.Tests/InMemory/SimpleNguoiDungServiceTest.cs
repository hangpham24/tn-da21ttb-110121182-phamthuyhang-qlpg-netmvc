using FluentAssertions;
using GymManagement.Web.Data.Models;
using System;
using Xunit;

namespace GymManagement.Tests.InMemory
{
    /// <summary>
    /// 🧪 SIMPLE IN-MEMORY TEST - BASIC MODEL TESTS
    /// Start with the simplest possible tests to verify approach works
    /// No database, no services, just basic model creation and validation
    /// </summary>
    public class SimpleNguoiDungServiceTest
    {
        [Fact]
        public void NguoiDung_CreateBasicUser_ShouldHaveCorrectProperties()
        {
            // 🎯 Arrange & Act - Create a basic user
            var user = new NguoiDung
            {
                Ho = "Test",
                Ten = "User",
                Email = "test@example.com",
                SoDienThoai = "0123456789",
                LoaiNguoiDung = "THANHVIEN",
                TrangThai = "ACTIVE",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                NgayTao = DateTime.Now
            };

            // 🔍 Assert - Verify properties
            user.Should().NotBeNull();
            user.Ho.Should().Be("Test");
            user.Ten.Should().Be("User");
            user.Email.Should().Be("test@example.com");
            user.SoDienThoai.Should().Be("0123456789");
            user.LoaiNguoiDung.Should().Be("THANHVIEN");
            user.TrangThai.Should().Be("ACTIVE");
        }

        [Fact]
        public void NguoiDung_CreateTrainer_ShouldHaveCorrectType()
        {
            // 🎯 Arrange & Act
            var trainer = new NguoiDung
            {
                Ho = "Trainer",
                Ten = "Test",
                Email = "trainer@example.com",
                LoaiNguoiDung = "TRAINER",
                TrangThai = "ACTIVE",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                NgayTao = DateTime.Now
            };

            // 🔍 Assert
            trainer.Should().NotBeNull();
            trainer.LoaiNguoiDung.Should().Be("TRAINER");
            trainer.Ho.Should().Be("Trainer");
            trainer.Ten.Should().Be("Test");
        }

        [Fact]
        public void NguoiDung_CreateWalkInGuest_ShouldHaveCorrectType()
        {
            // 🎯 Arrange & Act
            var guest = new NguoiDung
            {
                Ho = "Guest",
                Ten = "WalkIn",
                Email = "guest@example.com",
                LoaiNguoiDung = "VANGLAI",
                TrangThai = "ACTIVE",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                NgayTao = DateTime.Now
            };

            // 🔍 Assert
            guest.Should().NotBeNull();
            guest.LoaiNguoiDung.Should().Be("VANGLAI");
            guest.Ho.Should().Be("Guest");
            guest.Ten.Should().Be("WalkIn");
        }

        [Fact]
        public void DangKy_CreateBasicRegistration_ShouldHaveCorrectProperties()
        {
            // 🎯 Arrange & Act
            var dangKy = new DangKy
            {
                NguoiDungId = 1,
                LoaiDangKy = "THANHVIEN",
                NgayBatDau = DateOnly.FromDateTime(DateTime.Today),
                NgayKetThuc = DateOnly.FromDateTime(DateTime.Today.AddMonths(1)),
                PhiDangKy = 500000m,
                TrangThai = "ACTIVE",
                NgayTao = DateTime.Now
            };

            // 🔍 Assert
            dangKy.Should().NotBeNull();
            dangKy.NguoiDungId.Should().Be(1);
            dangKy.LoaiDangKy.Should().Be("THANHVIEN");
            dangKy.PhiDangKy.Should().Be(500000m);
            dangKy.TrangThai.Should().Be("ACTIVE");
        }

        [Fact]
        public void ThanhToan_CreateCashPayment_ShouldHaveCorrectProperties()
        {
            // 🎯 Arrange & Act
            var payment = new ThanhToan
            {
                DangKyId = 1,
                SoTien = 500000m,
                PhuongThuc = "CASH",
                TrangThai = "SUCCESS",
                NgayThanhToan = DateTime.Now,
                GhiChu = "Test payment"
            };

            // 🔍 Assert
            payment.Should().NotBeNull();
            payment.DangKyId.Should().Be(1);
            payment.SoTien.Should().Be(500000m);
            payment.PhuongThuc.Should().Be("CASH");
            payment.TrangThai.Should().Be("SUCCESS");
        }

        [Theory]
        [InlineData("THANHVIEN")]
        [InlineData("TRAINER")]
        [InlineData("VANGLAI")]
        [InlineData("ADMIN")]
        public void NguoiDung_CreateWithDifferentTypes_ShouldAcceptAllValidTypes(string userType)
        {
            // 🎯 Arrange & Act
            var user = new NguoiDung
            {
                Ho = "Test",
                Ten = "User",
                Email = "test@example.com",
                LoaiNguoiDung = userType,
                TrangThai = "ACTIVE",
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                NgayTao = DateTime.Now
            };

            // 🔍 Assert
            user.Should().NotBeNull();
            user.LoaiNguoiDung.Should().Be(userType);
        }

        [Theory]
        [InlineData("ACTIVE")]
        [InlineData("INACTIVE")]
        [InlineData("SUSPENDED")]
        public void NguoiDung_CreateWithDifferentStatuses_ShouldAcceptAllValidStatuses(string status)
        {
            // 🎯 Arrange & Act
            var user = new NguoiDung
            {
                Ho = "Test",
                Ten = "User",
                Email = "test@example.com",
                LoaiNguoiDung = "THANHVIEN",
                TrangThai = status,
                NgayThamGia = DateOnly.FromDateTime(DateTime.Today),
                NgayTao = DateTime.Now
            };

            // 🔍 Assert
            user.Should().NotBeNull();
            user.TrangThai.Should().Be(status);
        }
    }
}
