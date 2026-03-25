namespace ProductService.Application.DTOs;

/// <summary>Ürün oluşturma istek DTO'su.</summary>
public sealed record CreateProductRequest(
    string Name,
    string Description,
    decimal Price,
    int Stock
);
