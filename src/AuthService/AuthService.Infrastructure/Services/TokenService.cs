using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Services;

/// <summary>
/// ITokenService arayüzünün JWT implementasyonu.
///
/// DIP: Application katmanı ITokenService'e bağımlıdır; bu somut sınıfa değil.
/// SRP: Yalnızca JWT ve refresh token üretiminden sorumludur.
///
/// Güvenlik kararları:
/// - Access token: HMAC-SHA256 imzalı, 15 dakika ömürlü
/// - Refresh token: 32-byte kriptografik rastgele değer, 7 gün ömürlü
/// - Secret key ortam değişkeninden alınır (12-Factor III)
/// </summary>
public sealed class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <inheritdoc />
    public string GenerateAccessToken(AppUser user, IList<string> roles)
    {
        var jwtSettings = _configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        // Claim'leri oluştur
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new("fullName", user.FullName),
        };

        // Rolleri claim olarak ekle (Role-Based Authorization için)
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var expiry = DateTime.UtcNow.AddMinutes(
            double.Parse(jwtSettings["ExpiryInMinutes"] ?? "15"));

        var token = new JwtSecurityToken(
            issuer: jwtSettings["Issuer"],
            audience: jwtSettings["Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public RefreshToken GenerateRefreshToken(string userId)
    {
        // RFC 4086 uyumlu kriptografik rastgele byte üretimi
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        return new RefreshToken
        {
            Token = Convert.ToBase64String(randomBytes),
            UserId = userId,
            ExpiryDate = DateTime.UtcNow.AddDays(7),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };
    }
}
