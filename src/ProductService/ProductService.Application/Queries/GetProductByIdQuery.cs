using MediatR;
using ProductService.Application.DTOs;

namespace ProductService.Application.Queries;

/// <summary>Tek ürün getiren CQRS query'si.</summary>
public sealed record GetProductByIdQuery(Guid Id) : IRequest<ProductDto>;
