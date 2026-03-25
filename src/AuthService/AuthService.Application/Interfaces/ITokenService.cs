using AuthService.Domain.Entities;

namespace AuthService.Application.Interfaces;

/// <summary>
/// JWT access token ve refresh token üretim kontratı.
///
/// ISP: Bu arayüz yalnızca token işlemlerine odaklanır;
/// kullanıcı yönetimi gibi sorumlulukları içermez.
///
/// DIP: Üst katmanlar (Application) somut implementasyona değil,
/// bu soyutlamaya bağımlıdır.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Verilen kullanıcı için JWT access token üretir.
    /// </summary>
    /// <param name="user">Token sahibi kullanıcı.</param>
    /// <param name="roles">Kullanıcıya ait roller.</param>
    /// <returns>İmzalanmış JWT string.</returns>
    string GenerateAccessToken(AppUser user, IList<string> roles);

    /// <summary>
    /// Kriptografik olarak güvenli bir refresh token üretir.
    /// </summary>
    RefreshToken GenerateRefreshToken(string userId);
}
