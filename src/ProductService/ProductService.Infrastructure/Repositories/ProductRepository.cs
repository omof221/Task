using Microsoft.EntityFrameworkCore;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Infrastructure.Persistence;

namespace ProductService.Infrastructure.Repositories;

/// <summary>
/// IProductRepository'nin EF Core implementasyonu.
/// DIP: Application katmanı bu sınıfı bilmez; arayüzü kullanır.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly ProductDbContext _context;

    public ProductRepository(ProductDbContext context) => _context = context;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _context.Products
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken ct = default)
        => await _context.Products
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<Product>> GetActiveAsync(CancellationToken ct = default)
        => await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
        => await _context.Products.AddAsync(product, ct);

    public Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        // EF Core değişiklikleri takip eder;
        // AsNoTracking kullandığımız için Attach + Modified gerekir
        _context.Products.Attach(product);
        _context.Entry(product).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
        => _context.SaveChangesAsync(ct);
}
