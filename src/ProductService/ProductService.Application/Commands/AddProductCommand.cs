using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Commands;

/// Ürün ekleme CQRS command'i.
/// IRequest&lt;ProductDto&gt;: MediatR handler zincirini tetikler.
/// SRP: Yalnızca ürün oluşturma verisini taşır.

public sealed record AddProductCommand(
    string Name,
    string Description,
    decimal Price,
    int Stock
) : IRequest<ProductDto>;
