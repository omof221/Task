using Shared.Domain.Exceptions;

namespace ProductService.Domain.Exceptions;

/// <summary>
/// Ürün bulunamadığında fırlatılır.
/// DomainException'dan türer → ExceptionHandlingMiddleware 404 döner.
/// OCP: Middleware kodu değişmeden yeni exception tipi desteklenir.
/// </summary>
public sealed class ProductNotFoundException : DomainException
{
    public ProductNotFoundException(Guid id)
        : base($"Ürün bulunamadı: {id}", statusCode: 404) { }

    public ProductNotFoundException(string message)
        : base(message, statusCode: 404) { }
}
