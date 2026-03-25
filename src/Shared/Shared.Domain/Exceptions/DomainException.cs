namespace Shared.Domain.Exceptions;

/// <summary>
/// Tüm domain exception'larının türediği temel sınıf.
/// HTTP status code bilgisini taşır; böylece ExceptionHandlingMiddleware
/// domain-spesifik sınıfları bilmeden doğru HTTP yanıtı üretebilir.
///
/// OCP: Yeni domain exception'ları bu sınıftan türetilerek eklenir;
/// middleware kodu değişmez.
///
/// DIP: Shared.Infrastructure, servis-spesifik exception'lara değil
/// bu soyutlamaya bağımlıdır.
/// </summary>
public abstract class DomainException : Exception
{
    /// <summary>Bu exception için döndürülecek HTTP status kodu.</summary>
    public int StatusCode { get; }

    protected DomainException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    protected DomainException(string message, Exception innerException, int statusCode = 400)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }
}
