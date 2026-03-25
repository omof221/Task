using Microsoft.AspNetCore.Identity;

namespace AuthService.Domain.Entities;

/// <summary>
/// Uygulama kullanıcı entity'si.
/// ASP.NET Core Identity'nin IdentityUser sınıfı extend edilerek
/// domain'e özgü alanlar eklenmiştir (FullName, Role, RefreshTokens).
///
/// DIP: AppUser, framework'e bağımlı olsa da bu bağımlılık
/// Identity'nin zorunluluğundan kaynaklanmaktadır; altyapıdan izole edilmiştir.
/// </summary>
public class AppUser : IdentityUser
{
    /// <summary>Kullanıcının tam adı.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Rol bilgisi (Admin / User).
    /// Role-Based Authorization için kullanılır.
    /// </summary>
    public string Role { get; set; } = "User";

    /// <summary>Hesap oluşturma zamanı (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Kullanıcıya ait refresh token koleksiyonu.
    /// Navigation property — EF Core lazy loading desteği için virtual.
    /// </summary>
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
