using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Shared.Infrastructure.Authorization.Requirements;

namespace Shared.Infrastructure.Authorization.Handlers;

/// HasRoleRequirement için custom IAuthorizationHandler implementasyonu.
///
/// Standart RequireRole() yerine bu handler kullanılmasının nedenleri:
///   1. Yetkilendirme kararı loglara yazılır (gözlemlenebilirlik).
///   2. Rol karşılaştırması büyük/küçük harf duyarsızdır (OrdinalIgnoreCase).
///   3. Gelecekte ek iş kuralı eklenmesi için açık nokta sağlar (OCP).
///   4. DIP: ILogger arayüzüne bağımlı; somut logger sınıfına değil.
///
/// SRP: Yalnızca rol doğrulama + loglama sorumluluğuna sahiptir.
public sealed class HasRoleHandler : AuthorizationHandler<HasRoleRequirement>
{
    private readonly ILogger<HasRoleHandler> _logger;

    public HasRoleHandler(ILogger<HasRoleHandler> logger)
        => _logger = logger;

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasRoleRequirement           requirement)
    {
        // Kimlik doğrulanmamış kullanıcı → direkt reddet
        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            _logger.LogWarning(
                "Yetkilendirme reddedildi: Kullanıcı kimliği doğrulanmamış. " +
                "GerekliRoller=[{Roles}]",
                string.Join(", ", requirement.AllowedRoles));
            return Task.CompletedTask;
        }

        var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? context.User.FindFirstValue("sub")
                     ?? "bilinmiyor";

        // JWT'deki tüm role claim'lerini topla
        var userRoles = context.User
            .FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .ToArray();

        // İzin verilen rollerden herhangi biri kullanıcıda var mı?
        var hasRole = userRoles.Any(r =>
            requirement.AllowedRoles.Any(allowed =>
                string.Equals(r, allowed, StringComparison.OrdinalIgnoreCase)));

        if (hasRole)
        {
            _logger.LogInformation(
                "Yetkilendirme başarılı. UserId={UserId} " +
                "KullanıcıRolleri=[{UserRoles}] GerekliRoller=[{RequiredRoles}]",
                userId,
                string.Join(", ", userRoles),
                string.Join(", ", requirement.AllowedRoles));

            context.Succeed(requirement);
        }
        else
        {
            _logger.LogWarning(
                "Yetkilendirme reddedildi. UserId={UserId} " +
                "KullanıcıRolleri=[{UserRoles}] GerekliRoller=[{RequiredRoles}]",
                userId,
                string.Join(", ", userRoles),
                string.Join(", ", requirement.AllowedRoles));
        }

        return Task.CompletedTask;
    }
}
