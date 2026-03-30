using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces;


/// Refresh token veri erişim kontratı.

public interface IRefreshTokenRepository
{
    /// <summary>Token değerine göre aktif refresh token'ı getirir.</summary>
    Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait tüm aktif refresh token'ları getirir.</summary>
    Task<IEnumerable<RefreshToken>> GetActiveTokensByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Yeni refresh token kaydeder.</summary>
    Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

    /// <summary>Belirtilen refresh token'ı iptal eder (revoke).</summary>
    Task RevokeAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Kullanıcıya ait tüm refresh token'ları iptal eder (logout senaryosu).</summary>
    Task RevokeAllByUserIdAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Değişiklikleri veri tabanına kayıt eder.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
