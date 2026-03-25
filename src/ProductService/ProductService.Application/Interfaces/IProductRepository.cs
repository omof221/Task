using ProductService.Domain.Entities;

namespace ProductService.Application.Interfaces;

/// <summary>
/// Ürün veri erişim sözleşmesi.
/// ISP: Yalnızca ProductService'e özgü metotlar barındırır.
/// DIP: Application katmanı bu arayüze bağımlıdır, EF Core implementasyonuna değil.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    Task UpdateAsync(Product product, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
