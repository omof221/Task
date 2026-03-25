using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Commands;

/// <summary>
/// Ürün ekleme CQRS command'i.
/// IRequest&lt;ProductDto&gt;: MediatR handler zincirini tetikler.
/// SRP: Yalnızca ürün oluşturma verisini taşır.
/// </summary>
public sealed record AddProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock
) : IRequest<ProductDto>;
