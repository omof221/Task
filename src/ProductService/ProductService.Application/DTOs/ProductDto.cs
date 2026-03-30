namespace ProductService.Application.DTOs;

/// Ürün okuma DTO'su — domain entity'nin dışa açılan projeksiyonu
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
