using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using ProductService.Application.Extensions;
using ProductService.Infrastructure.Extensions;
using Shared.Infrastructure.Authorization.Handlers;
using Shared.Infrastructure.Authorization.Requirements;
using Shared.Infrastructure.Middleware;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── Application katmanı (CQRS / MediatR / Validation) ────────
builder.Services.AddProductApplication();

// ── Infrastructure katmanı (DB / Redis / Messaging) ───────────
// 12-Factor: Testing ortamında InMemory DB + Null Cache/Publisher
var isTesting = builder.Environment.IsEnvironment("Testing");
builder.Services.AddProductInfrastructure(builder.Configuration, isTesting);

// ── JWT Authentication (Lazy config — Options pattern) ────────
// AuthService ile aynı SecretKey/Issuer/Audience kullanılmalı
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer();

builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<Microsoft.Extensions.Configuration.IConfiguration>((options, config) =>
    {
        var jwtSection = config.GetSection("JwtSettings");
        var secret     = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JWT SecretKey yapılandırılmamış.");

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = jwtSection["Issuer"],
            ValidAudience            = jwtSection["Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(secret)),
            ClockSkew = TimeSpan.Zero
        };
    });

// ── Role-Based + Policy-Based Authorization ───────────────────
// Custom HasRoleRequirement + HasRoleHandler (Shared.Infrastructure)
// Yetkilendirme kararları handler'da loglanır (SRP/OCP/DIP).
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.Requirements.Add(new HasRoleRequirement("Admin")));

    options.AddPolicy("UserOrAdmin", policy =>
        policy.Requirements.Add(new HasRoleRequirement("User", "Admin")));
});

// Custom handler DI kaydı (IAuthorizationHandler arayüzü üzerinden)
builder.Services.AddSingleton<IAuthorizationHandler, HasRoleHandler>();

// ── Controllers + Swagger ─────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "ProductService API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = Microsoft.OpenApi.Models.ParameterLocation.Header
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── HealthChecks ──────────────────────────────────────────────
builder.Services.AddHealthChecks();

// ── Logging ───────────────────────────────────────────────────
// 12-Factor XI: stdout'a yaz; Seq/ELK dışarıdan toplar
builder.Logging.AddConsole();

// ─────────────────────────────────────────────────────────────
var app = builder.Build();
// ─────────────────────────────────────────────────────────────

// Global exception handler — RFC 7807 ProblemDetails
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ProductService v1"));
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Integration test erişimi için partial class
public partial class Program { }
