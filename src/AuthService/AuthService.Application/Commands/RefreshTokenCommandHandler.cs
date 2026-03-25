using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Application.Commands;

/// <summary>
/// RefreshTokenCommand'ı işleyen MediatR handler.
///
/// SRP: Yalnızca token yenileme iş akışından sorumludur.
///   1) Refresh token'ı veritabanında bul ve aktifliğini doğrula
///   2) Kullanıcıyı yükle
///   3) Eski token'ı iptal et (rotation stratejisi)
///   4) Yeni access token + refresh token üret
///   5) Yeni refresh token'ı kaydet
///
/// Token Rotation: Her kullanımda eski token revoke edilir;
/// bu sayede çalınan token'ların tekrar kullanımı engellenir.
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, TokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly UserManager<Domain.Entities.AppUser> _userManager;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        UserManager<Domain.Entities.AppUser> userManager,
        ITokenService tokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<TokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Token'ı bul ve doğrula
        var storedToken = await _refreshTokenRepository.GetByTokenAsync(request.RefreshToken, cancellationToken)
            ?? throw AuthException.InvalidToken();

        if (!storedToken.IsActive)
            throw AuthException.InvalidToken();

        // 2. Kullanıcıyı yükle
        var user = await _userManager.FindByIdAsync(storedToken.UserId)
            ?? throw AuthException.InvalidToken();

        // 3. Eski token'ı revoke et (Token Rotation Pattern)
        await _refreshTokenRepository.RevokeAsync(request.RefreshToken, cancellationToken);

        // 4. Yeni token'lar üret
        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = _tokenService.GenerateAccessToken(user, roles);
        var newRefreshToken = _tokenService.GenerateRefreshToken(user.Id);

        // 5. Yeni refresh token'ı kaydet
        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshToken.Token,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15)
        );
    }
}
