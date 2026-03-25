using AuthService.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Persistence;

/// <summary>
/// AuthService veritabanı bağlam sınıfı.
/// IdentityDbContext'ten türetilerek Microsoft Identity tabloları
/// (Users, Roles, UserRoles, Claims vb.) otomatik dahil edilir.
///
/// 12-Factor IV — Backing Services:
/// Bağlantı dizesi ortam değişkeninden alınır; kodda sabit değer yoktur.
/// </summary>
public class AuthDbContext : IdentityDbContext<AppUser, IdentityRole, string>
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options) { }

    /// <summary>Refresh token tablosu.</summary>
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // --- RefreshToken yapılandırması ---
        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(rt => rt.Id);

            entity.Property(rt => rt.Token)
                  .IsRequired()
                  .HasMaxLength(512);

            entity.HasIndex(rt => rt.Token)
                  .IsUnique();

            entity.Property(rt => rt.UserId)
                  .IsRequired();

            // Kullanıcı silindiğinde refresh token'ları da sil (Cascade)
            entity.HasOne(rt => rt.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(rt => rt.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // --- AppUser ek yapılandırması ---
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(u => u.FullName)
                  .HasMaxLength(200);

            entity.Property(u => u.Role)
                  .HasMaxLength(50)
                  .HasDefaultValue("User");
        });

        // Identity tablo adlarını özelleştir (isteğe bağlı)
        builder.Entity<AppUser>().ToTable("Users");
        builder.Entity<IdentityRole>().ToTable("Roles");
        builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
        builder.Entity<IdentityUserClaim<string>>().ToTable("UserClaims");
        builder.Entity<IdentityUserLogin<string>>().ToTable("UserLogins");
        builder.Entity<IdentityRoleClaim<string>>().ToTable("RoleClaims");
        builder.Entity<IdentityUserToken<string>>().ToTable("UserTokens");
    }
}
