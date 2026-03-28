using System.Text;
using AuthService.Application.Commands;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Infrastructure.Persistence;
using AuthService.Infrastructure.Repositories;
using AuthService.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Infrastructure.Authorization.Handlers;
using Shared.Infrastructure.Authorization.Requirements;
using Shared.Infrastructure.Middleware;

// ─────────────────────────────────────────────────────────────
// BUILDER
// 12-Factor I  — Tek kod tabanı, merkezi yapılandırma
// 12-Factor III — Config ortam değişkenlerinden okunur
// ─────────────────────────────────────────────────────────────
var builder = WebApplication.CreateBuilder(args);

// ── Veritabanı (SQL Server + EF Core) ────────────────────────
// 12-Factor IV — Backing services bağımsız servisler olarak ele alınır
// "Testing" ortamında InMemory kullanılır; SqlServer provider çakışması olmaz.
if (builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseInMemoryDatabase("AuthIntegrationTestDb"));
}
else
{
    builder.Services.AddDbContext<AuthDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
}

// ── Microsoft Identity ────────────────────────────────────────
// AppUser + IdentityRole, şifre politikaları, UserManager, RoleManager
builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    // Şifre kuralları
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    // Kullanıcı kuralları
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

// ── JWT Authentication ────────────────────────────────────────
// IConfiguration lazy okunur (Configure<JwtBearerOptions>) sayesinde
// WebApplicationFactory'nin ConfigureAppConfiguration override'ı
// middleware ilk çalışmadan önce uygulanmış olur.
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(); // Seçenekler aşağıda Configure<> ile bağlanır

// Named options üzerinden IConfiguration'ı DI resolution anında oku
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IConfiguration>((options, config) =>
    {
        var jwtSection = config.GetSection("JwtSettings");
        var secret = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ── Role-Based + Policy-Based Authorization ───────────────────
// Ekstra değerlendirme kriteri: farklı yetki seviyeleri
// Custom HasRoleRequirement + HasRoleHandler kullanılır.
// Standart RequireRole()'den farkı: yetkilendirme kararları
// loglara yazılır ve iş kuralı handler'da merkezi yönetilir (OCP/SRP).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new HasRoleRequirement("Admin")));

    options.AddPolicy("UserOrAdmin", policy =>
        policy.Requirements.Add(new HasRoleRequirement("User", "Admin")));
});

// Custom handler DI'a kayıt (DIP: IAuthorizationHandler arayüzü üzerinden)
builder.Services.AddSingleton<IAuthorizationHandler, HasRoleHandler>();

// ── MediatR (CQRS) ───────────────────────────────────────────
// LoginCommand, RegisterCommand, RefreshTokenCommand handler'larını tarar
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly));

// ── Repository & Service DI Kayıtları ────────────────────────
// DIP: Interface'lere bağımlılık; implementasyonlar burada bağlanır
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// ── Controllers & Swagger ────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AuthService API",
        Version = "v1",
        Description = "JWT tabanlı kimlik doğrulama ve yetkilendirme servisi."
    });

    // Swagger'a JWT Bearer desteği ekle
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT token giriniz: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── Health Checks ─────────────────────────────────────────────
// SqlServer health check sadece gerçek DB bağlantısı olan ortamlarda aktif
var healthChecks = builder.Services.AddHealthChecks();
if (!builder.Environment.IsEnvironment("Testing"))
{
    healthChecks.AddSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty,
        name: "sqlserver",
        tags: ["db", "auth"]);
}

// ─────────────────────────────────────────────────────────────
// APP (Middleware Pipeline)
// ─────────────────────────────────────────────────────────────
var app = builder.Build();

// Global hata yakalama (Shared.Infrastructure)
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
  
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AuthService v1");

        
    });
}

// 12-Factor VII — Port Binding
//app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ── Otomatik Migration (Development) ─────────────────────────
// 12-Factor X — Dev/Prod parity
// "Testing" ortamında InMemory DB kullanıldığı için migration atlanır.
if (app.Environment.IsDevelopment() && !app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
}

await app.RunAsync();

// Integration test'lerden erişim için partial class
public partial class Program { }
