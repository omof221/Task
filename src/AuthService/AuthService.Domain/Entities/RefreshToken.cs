namespace AuthService.Domain.Entities;

/// <summary>
/// Refresh token domain entity'si.
/// Access token süresi dolduğunda yeni token üretmek için kullanılır.
///
/// SRP: Yalnızca token verilerini tutar; token doğrulama mantığı servislerdedir.
/// </summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Kriptografik olarak güvenli, benzersiz token değeri.</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>Token'ın bağlı olduğu kullanıcı Id (IdentityUser.Id).</summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>Token'ın geçerlilik bitiş zamanı (UTC).</summary>
    public DateTime ExpiryDate { get; set; }

    /// <summary>Token'ın iptal edilip edilmediğini belirtir.</summary>
    public bool IsRevoked { get; set; } = false;

    /// <summary>Token'ın oluşturulma zamanı (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Navigation property — token sahibi kullanıcı.</summary>
    public virtual AppUser? User { get; set; }

    /// <summary>Token'ın hâlâ aktif olup olmadığını kontrol eder.</summary>
    public bool IsActive => !IsRevoked && ExpiryDate > DateTime.UtcNow;
}
