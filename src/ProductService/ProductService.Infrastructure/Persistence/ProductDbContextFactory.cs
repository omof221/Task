using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProductService.Infrastructure.Persistence;

/// EF Core design-time (dotnet ef migrations) için DbContext factory.
/// Migration araçları uygulama başlangıcını (Redis, RabbitMQ vb.) tetiklemeden
/// doğrudan ProductDbContext oluşturabilir.
/// Bağlantı dizesi PRODUCT_DB_CONNECTION env var üzerinden veya
/// varsayılan .\SQLEXPRESS adresi ile sağlanır.
public sealed class ProductDbContextFactory : IDesignTimeDbContextFactory<ProductDbContext>
{
    public ProductDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("PRODUCT_DB_CONNECTION")
            ?? "Server=.\\SQLEXPRESS;Database=ProductDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        var optionsBuilder = new DbContextOptionsBuilder<ProductDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ProductDbContext(optionsBuilder.Options);
    }
}
