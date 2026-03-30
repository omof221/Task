namespace ProductService.Application.DTOs;

/// Ürün güncelleme istek DTO'su
public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock
);
