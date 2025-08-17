using Xunit;
using Moq;
using FluentAssertions;
using GymManagement.Web.Services;
using GymManagement.Web.Data;
using GymManagement.Web.Data.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GymManagement.Tests.Unit.Services;

public class AuthServiceTests : IDisposable
{
    private readonly Mock<IPasswordService> _mockPasswordService;
    private readonly GymDbContext _context;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _mockPasswordService = new Mock<IPasswordService>();
        
        var options = new DbContextOptionsBuilder<GymDbContext>()
            .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
            .Options;
        
        _context = new GymDbContext(options);
        _authService = new AuthService(_context, _mockPasswordService.Object);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnUser_WhenCredentialsAreValid()
    {
        // Arrange
        var username = "testuser";
        var password = "Test@123";
        var salt = "salt";
        var hashedPassword = "hashedPassword";
        
        var user = new TaiKhoan
        {
            Id = "1",
            TenDangNhap = username,
            Email = "test@example.com",
            Salt = salt,
            MatKhauHash = hashedPassword,
            KichHoat = true,
            NgayTao = DateTime.UtcNow
        };
        
        _context.TaiKhoans.Add(user);
        await _context.SaveChangesAsync();
        
        _mockPasswordService.Setup(x => x.VerifyPassword(password, salt, hashedPassword))
            .Returns(true);

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be("1");
        result.TenDangNhap.Should().Be(username);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserNotFound()
    {
        // Arrange
        var username = "nonexistent";
        var password = "Test@123";

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenPasswordIsInvalid()
    {
        // Arrange
        var username = "testuser";
        var password = "WrongPassword";
        var salt = "salt";
        var hashedPassword = "hashedPassword";
        
        var user = new TaiKhoan
        {
            Id = "1",
            TenDangNhap = username,
            Email = "test@example.com",
            Salt = salt,
            MatKhauHash = hashedPassword,
            KichHoat = true,
            NgayTao = DateTime.UtcNow
        };
        
        _context.TaiKhoans.Add(user);
        await _context.SaveChangesAsync();
        
        _mockPasswordService.Setup(x => x.VerifyPassword(password, salt, hashedPassword))
            .Returns(false);

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldReturnNull_WhenUserIsInactive()
    {
        // Arrange
        var username = "testuser";
        var password = "Test@123";
        var salt = "salt";
        var hashedPassword = "hashedPassword";
        
        var user = new TaiKhoan
        {
            Id = "1",
            TenDangNhap = username,
            Email = "test@example.com",
            Salt = salt,
            MatKhauHash = hashedPassword,
            KichHoat = false, // User is inactive
            NgayTao = DateTime.UtcNow
        };
        
        _context.TaiKhoans.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.AuthenticateAsync(username, password);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnUser_WhenExists()
    {
        // Arrange
        var userId = "1";
        var user = new TaiKhoan
        {
            Id = userId,
            TenDangNhap = "testuser",
            Email = "test@example.com",
            Salt = "salt",
            MatKhauHash = "hash",
            KichHoat = true,
            NgayTao = DateTime.UtcNow
        };
        
        _context.TaiKhoans.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(userId);
    }

    [Fact]
    public async Task GetUserByIdAsync_ShouldReturnNull_WhenNotExists()
    {
        // Arrange
        var userId = "999";

        // Act
        var result = await _authService.GetUserByIdAsync(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnTrue_WhenSuccessful()
    {
        // Arrange
        var password = "Test@123";
        var salt = "generatedSalt";
        var hashedPassword = "hashedPassword";
        
        var newUser = new TaiKhoan
        {
            Id = Guid.NewGuid().ToString(),
            TenDangNhap = "newuser",
            Email = "newuser@example.com",
            NgayTao = DateTime.UtcNow
        };
        
        _mockPasswordService.Setup(x => x.GenerateSalt()).Returns(salt);
        _mockPasswordService.Setup(x => x.HashPassword(password, salt)).Returns(hashedPassword);

        // Add a role to the database
        _context.VaiTros.Add(new VaiTro { Id = "1", TenVaiTro = "Member" });
        await _context.SaveChangesAsync();

        // Act
        var result = await _authService.CreateUserAsync(newUser, password);

        // Assert
        result.Should().BeTrue();
        
        var createdUser = await _context.TaiKhoans.FirstOrDefaultAsync(u => u.TenDangNhap == "newuser");
        createdUser.Should().NotBeNull();
        createdUser!.Salt.Should().Be(salt);
        createdUser.MatKhauHash.Should().Be(hashedPassword);
    }

    [Fact]
    public async Task CreateUserAsync_ShouldReturnFalse_WhenUsernameExists()
    {
        // Arrange
        var existingUser = new TaiKhoan
        {
            Id = "1",
            TenDangNhap = "existinguser",
            Email = "existing@example.com",
            Salt = "salt",
            MatKhauHash = "hash",
            NgayTao = DateTime.UtcNow
        };
        
        _context.TaiKhoans.Add(existingUser);
        await _context.SaveChangesAsync();
        
        var newUser = new TaiKhoan
        {
            Id = Guid.NewGuid().ToString(),
            TenDangNhap = "existinguser", // Same username
            Email = "new@example.com",
            NgayTao = DateTime.UtcNow
        };

        // Act
        var result = await _authService.CreateUserAsync(newUser, "Test@123");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CreateClaimsPrincipalAsync_ShouldReturnCorrectClaims()
    {
        // Arrange
        var vaiTroMember = new VaiTro { Id = "1", TenVaiTro = "Member" };
        var vaiTroAdmin = new VaiTro { Id = "2", TenVaiTro = "Admin" };
        
        _context.VaiTros.AddRange(vaiTroMember, vaiTroAdmin);
        await _context.SaveChangesAsync();
        
        var user = new TaiKhoan
        {
            Id = "1",
            TenDangNhap = "testuser",
            Email = "test@example.com",
            NgayTao = DateTime.UtcNow
        };
        
        _context.TaiKhoans.Add(user);
        await _context.SaveChangesAsync();
        
        // Add roles to user
        _context.TaiKhoanVaiTros.AddRange(
            new TaiKhoanVaiTro { TaiKhoanId = user.Id, VaiTroId = vaiTroMember.Id },
            new TaiKhoanVaiTro { TaiKhoanId = user.Id, VaiTroId = vaiTroAdmin.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var principal = await _authService.CreateClaimsPrincipalAsync(user);

        // Assert
        principal.Should().NotBeNull();
        principal.Identity.Should().NotBeNull();
        principal.Identity!.Name.Should().Be("testuser");
        
        var claims = principal.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == "1");
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "testuser");
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "test@example.com");
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Member");
        claims.Should().Contain(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
