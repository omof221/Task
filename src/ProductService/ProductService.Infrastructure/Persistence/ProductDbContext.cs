using Microsoft.EntityFrameworkCore;
using ProductService.Domain.Entities;
using Shared.Domain.BaseEntities;

namespace ProductService.Infrastructure.Persistence;

/// <summary>
/// ProductService EF Core DB context'i.
/// 12-Factor IV: Veritabanı bağlantısı environment variable üzerinden gelir.
/// </summary>
public class ProductDbContext : DbContext
{
    public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("product");

        modelBuilder.Entity<Product>(entity =>
        {
            entity.ToTable("Products");
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(p => p.Description)
                .HasMaxLength(1000);

            entity.Property(p => p.Price)
                .HasPrecision(18, 2);

            entity.HasIndex(p => p.Name);
            entity.HasIndex(p => p.IsActive);
        });
    }

    /// <summary>
    /// SaveChanges override — BaseEntity.UpdatedAt otomatik güncellenir.
    /// SRP: Audit mantığı context'te merkezi olarak yönetilir.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Modified)
                entry.Entity.SetUpdatedAt();
        }
        return base.SaveChangesAsync(ct);
    }
}
