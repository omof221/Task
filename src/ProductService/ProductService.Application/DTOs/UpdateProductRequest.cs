namespace ProductService.Application.DTOs;

/// <summary>Ürün güncelleme istek DTO'su.</summary>
public sealed record UpdateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock
);
