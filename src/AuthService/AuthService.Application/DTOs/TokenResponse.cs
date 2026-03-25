namespace AuthService.Application.DTOs;

/// <summary>
/// Başarılı kimlik doğrulama sonucunda dönen token yanıtı.
/// Access token kısa ömürlü (15 dk), refresh token uzun ömürlüdür (7 gün).
/// </summary>
public record TokenResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiry,
    string TokenType = "Bearer"
);
