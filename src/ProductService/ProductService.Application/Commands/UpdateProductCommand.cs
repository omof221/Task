using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Commands;

/// Ürün güncelleme CQRS command'i.
/// JWT [Authorize] gerektiren tek write işlemi.
/// SRP: Yalnızca güncelleme verisini taşır.
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock
) : IRequest<ProductDto>;
