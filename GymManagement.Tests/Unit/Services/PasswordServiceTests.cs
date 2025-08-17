using Xunit;
using FluentAssertions;
using GymManagement.Web.Services;

namespace GymManagement.Tests.Unit.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService;

    public PasswordServiceTests()
    {
        _passwordService = new PasswordService();
    }

    [Fact]
    public void GenerateSalt_ShouldReturnNonEmptyString()
    {
        // Act
        var salt = _passwordService.GenerateSalt();

        // Assert
        salt.Should().NotBeNullOrEmpty();
        salt.Length.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GenerateSalt_ShouldReturnDifferentValuesEachTime()
    {
        // Act
        var salt1 = _passwordService.GenerateSalt();
        var salt2 = _passwordService.GenerateSalt();

        // Assert
        salt1.Should().NotBe(salt2);
    }

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyString()
    {
        // Arrange
        var password = "Test@123";
        var salt = _passwordService.GenerateSalt();

        // Act
        var hash = _passwordService.HashPassword(password, salt);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData("Test@123")]
    [InlineData("ComplexP@ssw0rd!")]
    [InlineData("Simple123")]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordIsCorrect(string password)
    {
        // Arrange
        var salt = _passwordService.GenerateSalt();
        var hash = _passwordService.HashPassword(password, salt);

        // Act
        var result = _passwordService.VerifyPassword(password, salt, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordIsIncorrect()
    {
        // Arrange
        var password = "Test@123";
        var wrongPassword = "WrongPassword";
        var salt = _passwordService.GenerateSalt();
        var hash = _passwordService.HashPassword(password, salt);

        // Act
        var result = _passwordService.VerifyPassword(wrongPassword, salt, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenSaltIsWrong()
    {
        // Arrange
        var password = "Test@123";
        var salt = _passwordService.GenerateSalt();
        var wrongSalt = _passwordService.GenerateSalt();
        var hash = _passwordService.HashPassword(password, salt);

        // Act
        var result = _passwordService.VerifyPassword(password, wrongSalt, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldGenerateSameHash_ForSamePasswordAndSalt()
    {
        // Arrange
        var password = "Test@123";
        var salt = _passwordService.GenerateSalt();

        // Act
        var hash1 = _passwordService.HashPassword(password, salt);
        var hash2 = _passwordService.HashPassword(password, salt);

        // Assert
        hash1.Should().Be(hash2);
    }
}
