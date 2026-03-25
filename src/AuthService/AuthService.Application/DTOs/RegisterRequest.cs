namespace AuthService.Application.DTOs;

/// <summary>
/// Kayıt isteği veri transfer nesnesi.
/// </summary>
public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role = "User"
);
