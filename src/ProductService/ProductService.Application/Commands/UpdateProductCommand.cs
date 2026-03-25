using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Commands;

/// <summary>
/// Ürün güncelleme CQRS command'i.
/// JWT [Authorize] gerektiren tek write işlemi.
/// SRP: Yalnızca güncelleme verisini taşır.
/// </summary>
public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock
) : IRequest<ProductDto>;
