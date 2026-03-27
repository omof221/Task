using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthService.Infrastructure.Persistence;


/// Migration araçları tam uygulama başlangıcı olmadan AuthDbContext oluşturur.

public sealed class AuthDbContextFactory : IDesignTimeDbContextFactory<AuthDbContext>
{
    public AuthDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("AUTH_DB_CONNECTION")
            ?? "Server=.\\SQLEXPRESS;Database=AuthDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AuthDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AuthDbContext(optionsBuilder.Options);
    }
}
