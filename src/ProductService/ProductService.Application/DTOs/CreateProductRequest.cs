namespace ProductService.Application.DTOs;

/// Ürün oluşturma istek DTO'su.
public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock
);
