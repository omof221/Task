namespace AuthService.Application.DTOs;

/// <summary>
/// Giriş isteği veri transfer nesnesi.
/// Controller → MediatR Command arasındaki köprüdür.
/// </summary>
public record LoginRequest(
    string Email,
    string Password
);
