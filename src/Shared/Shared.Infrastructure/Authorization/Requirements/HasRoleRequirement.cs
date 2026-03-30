using Microsoft.AspNetCore.Authorization;

namespace Shared.Infrastructure.Authorization.Requirements;

/// Custom Policy-Based Authorization gereksinimidir.
/// Role-Based Authorization'dan farkı: bir veya birden fazla rolü
/// tek bir gereksinim nesnesiyle temsil edebilir ve handler'da
/// özel iş kuralı + loglama uygulanabilir.
///
/// ISP: Yalnızca rol bilgisini taşır; doğrulama mantığı handler'dadır.
/// OCP: Yeni rol kombinasyonları bu sınıfı değiştirmeden
///      yeni HasRoleRequirement(...)  çağrısıyla eklenir.
public sealed class HasRoleRequirement : IAuthorizationRequirement
{
    /// Bu gereksinimi karşılayan izin verilen roller.
    public IReadOnlyList<string> AllowedRoles { get; }

    public HasRoleRequirement(params string[] allowedRoles)
    {
        if (allowedRoles is null || allowedRoles.Length == 0)
            throw new ArgumentException("En az bir rol belirtilmelidir.", nameof(allowedRoles));

        AllowedRoles = allowedRoles;
    }
}
