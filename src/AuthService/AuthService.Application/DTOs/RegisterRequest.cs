namespace AuthService.Application.DTOs;

public record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string Role = "User"
);
