using Shared.Domain.Exceptions;

namespace AuthService.Domain.Exceptions;

/// <summary>
/// AuthService domain'ine özgü hata sınıfı.
/// DomainException'dan türetilir; ExceptionHandlingMiddleware
/// StatusCode'u okuyarak doğru HTTP yanıtı üretir.
///
/// OCP: Yeni hata türleri bu sınıftan türetilerek sisteme eklenir;
/// mevcut kod değiştirilmez.
/// </summary>
public class AuthException : DomainException
{
    public AuthException(string message, int statusCode = 400)
        : base(message, statusCode) { }

    public AuthException(string message, Exception innerException, int statusCode = 400)
        : base(message, innerException, statusCode) { }

    // --- Fabrika metotları ---

    public static AuthException InvalidCredentials()
        => new("Kullanıcı adı veya şifre hatalı.", 401);

    public static AuthException UserNotFound(string email)
        => new($"Kullanıcı bulunamadı: {email}", 404);

    public static AuthException InvalidToken()
        => new("Geçersiz veya süresi dolmuş token.", 401);

    public static AuthException EmailAlreadyExists(string email)
        => new($"Bu e-posta adresi zaten kullanımda: {email}", 409);

    public static AuthException RegistrationFailed(IEnumerable<string> errors)
        => new($"Kayıt başarısız: {string.Join(", ", errors)}", 400);
}
