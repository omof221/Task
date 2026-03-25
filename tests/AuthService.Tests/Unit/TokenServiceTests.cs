using AuthService.Domain.Entities;
using AuthService.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests.Unit;

/// <summary>
/// TokenService unit testleri.
/// Bağımlılık: Sadece IConfiguration — EF Core, UserManager YOK.
///
/// Test stratejisi:
/// - GenerateAccessToken → geçerli JWT formatı üretmeli
/// - GenerateRefreshToken → benzersiz, süresi geçerli token
/// </summary>
public class TokenServiceTests
{
    private readonly TokenService _tokenService;
    private readonly AppUser _testUser;

    public TokenServiceTests()
    {
        // Test için in-memory configuration
        var inMemorySettings = new Dictionary<string, string?>
        {
            ["JwtSettings:SecretKey"] = "test-super-secret-key-minimum-32-chars!!",
            ["JwtSettings:Issuer"] = "AuthService",
            ["JwtSettings:Audience"] = "MicroserviceSolution",
            ["JwtSettings:ExpiryInMinutes"] = "15"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _tokenService = new TokenService(configuration);

        _testUser = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "test@example.com",
            UserName = "test@example.com",
            FullName = "Test User",
            Role = "User"
        };
    }

    [Fact]
    public void GenerateAccessToken_WithValidUser_ShouldReturnNonEmptyToken()
    {
        // Arrange
        var roles = new List<string> { "User" };

        // Act
        var token = _tokenService.GenerateAccessToken(_testUser, roles);

        // Assert
        token.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateAccessToken_ShouldReturnValidJwtFormat()
    {
        // Arrange
        var roles = new List<string> { "User" };

        // Act
        var token = _tokenService.GenerateAccessToken(_testUser, roles);

        // Assert — JWT 3 noktayla ayrılmış üç bölümden oluşur
        var parts = token.Split('.');
        parts.Should().HaveCount(3, "JWT header.payload.signature formatında olmalı");
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnTokenWithFutureExpiry()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken(_testUser.Id);

        // Assert
        refreshToken.Should().NotBeNull();
        refreshToken.Token.Should().NotBeNullOrEmpty();
        refreshToken.ExpiryDate.Should().BeAfter(DateTime.UtcNow);
        refreshToken.UserId.Should().Be(_testUser.Id);
        refreshToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void GenerateRefreshToken_CalledTwice_ShouldReturnUniqueTokens()
    {
        // Act
        var token1 = _tokenService.GenerateRefreshToken(_testUser.Id);
        var token2 = _tokenService.GenerateRefreshToken(_testUser.Id);

        // Assert — her token kriptografik olarak benzersiz olmalı
        token1.Token.Should().NotBe(token2.Token);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldExpireInSevenDays()
    {
        // Act
        var refreshToken = _tokenService.GenerateRefreshToken(_testUser.Id);

        // Assert — 7 gün ± 1 dakika tolerans
        var expectedExpiry = DateTime.UtcNow.AddDays(7);
        refreshToken.ExpiryDate.Should().BeCloseTo(expectedExpiry, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateAccessToken_WithAdminRole_ShouldIncludeRoleClaim()
    {
        // Arrange
        var roles = new List<string> { "Admin" };

        // Act
        var token = _tokenService.GenerateAccessToken(_testUser, roles);

        // Assert — Token'ı decode edip claim içeriğini doğrula
        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        var roleClaim = jwtToken.Claims.FirstOrDefault(c =>
            c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            || c.Type == "role");

        roleClaim.Should().NotBeNull("Admin rolü claim olarak eklenmeli");
        roleClaim!.Value.Should().Be("Admin");
    }
}
