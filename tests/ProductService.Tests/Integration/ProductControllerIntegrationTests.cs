using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using ProductService.Application.DTOs;

namespace ProductService.Tests.Integration;

// ── Paylaşımlı test factory ─────────────────────────────────────────
// IClassFixture ile tüm testler aynı factory/app instance'ını kullanır.
// Böylece aynı InMemory DB tüm requestler için paylaşılır.
public class ProductWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestJwtSecret = "integration-test-secret-key-min-32-chars!!";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting(WebHostDefaults.EnvironmentKey, "Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"]       = TestJwtSecret,
                ["JwtSettings:Issuer"]          = "AuthService",
                ["JwtSettings:Audience"]        = "MicroserviceSolution",
                ["JwtSettings:ExpiryMinutes"]   = "15"
            });
        });
    }

    public string JwtSecret => TestJwtSecret;
}


/// ProductController integration testleri.
/// Testing ortamı: InMemory DB + NullCache + NullEventPublisher.

public class ProductControllerIntegrationTests
    : IClassFixture<ProductWebApplicationFactory>
{
    private readonly ProductWebApplicationFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ProductControllerIntegrationTests(ProductWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    // ── GET /api/products ─────────────────────────────────────

    [Fact]
    public async Task GetAll_Returns200WithList()
    {
        var response = await _client.GetAsync("/api/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content  = await response.Content.ReadAsStringAsync();
        var products = JsonSerializer.Deserialize<List<ProductDto>>(content, JsonOptions);
        products.Should().NotBeNull();
    }

    // ── POST /api/products ────────────────────────────────────

    [Fact]
    public async Task Add_ValidProduct_Returns201WithProductDto()
    {
        var req = new CreateProductRequest("Valid Product", "Description", 99.99m, 5);

        var response = await _client.PostAsJsonAsync("/api/products", req);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var product = JsonSerializer.Deserialize<ProductDto>(content, JsonOptions);
        product.Should().NotBeNull();
        product!.Name.Should().Be("Valid Product");
        product.Price.Should().Be(99.99m);
        product.Id.Should().NotBeEmpty();
        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Add_EmptyName_Returns400()
    {
        var req = new CreateProductRequest("", "Description", 10m, 5);

        var response = await _client.PostAsJsonAsync("/api/products", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Add_NegativePrice_Returns400()
    {
        var req = new CreateProductRequest("Product", "Description", -5m, 5);

        var response = await _client.PostAsJsonAsync("/api/products", req);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── GET /api/products/{id} ────────────────────────────────

    [Fact]
    public async Task GetById_AfterAdd_Returns200()
    {
        // 1. Ürün ekle
        var addResp = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Single Product", "Desc", 250m, 1));
        addResp.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "GetById testi için önce ürün eklenmeli");

        var addBody  = await addResp.Content.ReadAsStringAsync();
        var created  = JsonSerializer.Deserialize<ProductDto>(addBody, JsonOptions);
        created.Should().NotBeNull();

        // 2. ID ile getir
        var response = await _client.GetAsync($"/api/products/{created!.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Single Product");
    }

    [Fact]
    public async Task GetById_NonExistingProduct_Returns404()
    {
        var response = await _client.GetAsync($"/api/products/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT /api/products/{id} — JWT [Authorize] ──────────────

    [Fact]
    public async Task Update_WithoutToken_Returns401()
    {
        var req      = new UpdateProductRequest("Updated", "Desc", 100m, 10);
        var response = await _client.PutAsJsonAsync($"/api/products/{Guid.NewGuid()}", req);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Update_WithValidToken_ExistingProduct_Returns200()
    {
        // 1. Ürün ekle
        var addResp = await _client.PostAsJsonAsync("/api/products",
            new CreateProductRequest("Update Me", "Original Desc", 50m, 5));
        addResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var addBody = await addResp.Content.ReadAsStringAsync();
        var created = JsonSerializer.Deserialize<ProductDto>(addBody, JsonOptions);

        // 2. JWT ile update
        var token = GenerateTestJwt(_factory.JwtSecret);

        using var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var updateReq = new UpdateProductRequest("Updated Product", "New Desc", 75m, 8);
        var response  = await authedClient.PutAsJsonAsync(
            $"/api/products/{created!.Id}", updateReq);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Updated Product");
    }

    // ── /health ───────────────────────────────────────────────

    [Fact]
    public async Task Health_Returns200()
    {
        var response = await _client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── JWT üretici ───────────────────────────────────────────

    private static string GenerateTestJwt(string secret)
    {
        var key   = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                        System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                        key, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Email, "testuser@test.com"),
            new System.Security.Claims.Claim(
                System.Security.Claims.ClaimTypes.Role, "User")
        };

        var token = new System.IdentityModel.Tokens.Jwt.JwtSecurityToken(
            issuer:             "AuthService",
            audience:           "MicroserviceSolution",
            claims:             claims,
            expires:            DateTime.UtcNow.AddMinutes(15),
            signingCredentials: creds);

        return new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler().WriteToken(token);
    }
}
