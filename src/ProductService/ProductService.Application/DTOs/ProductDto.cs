namespace ProductService.Application.DTOs;

/// <summary>Ürün okuma DTO'su — domain entity'nin dışa açılan projeksiyonu.</summary>
public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    int Stock,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
