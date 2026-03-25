using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Queries;

/// <summary>
/// Tüm aktif ürünleri listeleyen CQRS query'si.
/// CQRS: Read tarafı — DB'yi veya cache'i okur, değişiklik yapmaz.
/// </summary>
public sealed record GetProductsQuery : IRequest<IReadOnlyList<ProductDto>>;
