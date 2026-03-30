using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Queries;

/// Tek ürün getiren CQRS query'si.
public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
