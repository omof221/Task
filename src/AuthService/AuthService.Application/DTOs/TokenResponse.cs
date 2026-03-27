namespace AuthService.Application.DTOs;

public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    string TokenType = "Bearer"
);
