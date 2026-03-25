using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AuthService.Tests.Integration;

/// <summary>
/// AuthController integration testleri.
/// WebApplicationFactory ile gerçek HTTP pipeline test edilir.
/// Veritabanı olarak SQLite In-Memory kullanılır (PostgreSQL yerine —
/// sadece integration test ortamında).
///
/// Test senaryoları:
/// 1) Register → 201 Created + TokenResponse
/// 2) Login    → 200 OK + TokenResponse
/// 3) Refresh  → 200 OK + yeni TokenResponse
/// 4) Me       → 401 Unauthorized (token olmadan)
/// 5) Me       → 200 OK (geçerli token ile)
/// </summary>
public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            // "Testing" ortamı → Migration ve NpgSql health check çalışmaz
            // WebHostDefaults.EnvironmentKey = "environment"
            builder.UseSetting(Microsoft.AspNetCore.Hosting.WebHostDefaults.EnvironmentKey, "Testing");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["JwtSettings:SecretKey"] = "integration-test-secret-key-min-32-chars!!",
                    ["JwtSettings:Issuer"] = "AuthService",
                    ["JwtSettings:Audience"] = "MicroserviceSolution",
                    ["JwtSettings:ExpiryInMinutes"] = "15",
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=testdb"
                });
            });

            // Program.cs "Testing" ortamını algılar ve otomatik InMemory kullanır.
            // Ayrıca servis değişimine gerek yoktur.
        });

        // InMemory DB otomatik oluşur — EnsureCreated çağrısına gerek yok
        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ShouldReturn201WithToken()
    {
        // Arrange
        var request = new RegisterRequest(
            "Integration Test User",
            $"inttest_{Guid.NewGuid():N}@test.com",
            "TestPass123!",
            "User"
        );

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, JsonOptions);

        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
        tokenResponse.RefreshToken.Should().NotBeNullOrEmpty();
        tokenResponse.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldReturn200WithToken()
    {
        // Arrange — önce kayıt ol
        var email = $"login_test_{Guid.NewGuid():N}@test.com";
        var password = "LoginPass123!";

        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Login Test", email, password));

        var loginRequest = new LoginRequest(email, password);

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(content, JsonOptions);

        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldReturn401()
    {
        // Arrange
        var email = $"wrong_pass_{Guid.NewGuid():N}@test.com";
        await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Wrong Pass Test", email, "CorrectPass123!"));

        var loginRequest = new LoginRequest(email, "WrongPassword!");

        // Act
        var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithoutToken_ShouldReturn401()
    {
        // Act
        var response = await _client.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Me_WithValidToken_ShouldReturn200WithUserInfo()
    {
        // Arrange — kayıt ol ve token al
        var email = $"me_test_{Guid.NewGuid():N}@test.com";
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register",
            new RegisterRequest("Me Test User", email, "MeTestPass123!"));

        // Register başarılı olmalı
        registerResponse.StatusCode.Should().Be(HttpStatusCode.Created,
            because: "Me testi için önce başarılı kayıt gereklidir.");

        var registerContent = await registerResponse.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(registerContent, JsonOptions);

        tokenResponse.Should().NotBeNull();
        tokenResponse!.AccessToken.Should().NotBeNullOrWhiteSpace();

        // Authenticated istek için ayrı client (shared header kirlenmesini önler)
        using var authedClient = _factory.CreateClient();
        authedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResponse.AccessToken);

        // Act
        var response = await authedClient.GetAsync("/api/auth/me");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("email");
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var email = $"duplicate_{Guid.NewGuid():N}@test.com";
        var request = new RegisterRequest("Duplicate User", email, "DupPass123!");

        await _client.PostAsJsonAsync("/api/auth/register", request);

        // Act — aynı email ile tekrar kayıt
        var response = await _client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
