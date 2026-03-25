using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Application.Commands;

/// <summary>
/// LoginCommand'ı işleyen MediatR handler.
///
/// SRP: Yalnızca giriş iş akışından sorumludur.
///   1) Kullanıcıyı e-posta ile bul
///   2) Şifreyi doğrula
///   3) Rolleri getir
///   4) Access token + Refresh token üret
///   5) Refresh token'ı kaydet
///
/// DIP: UserManager, ITokenService, IRefreshTokenRepository
///      soyutlamalar üzerinden enjekte edilir; somut sınıflara bağımlılık yoktur.
/// </summary>
public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, TokenResponse>
{
    private readonly UserManager<Domain.Entities.AppUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LoginCommandHandler(
        UserManager<Domain.Entities.AppUser> userManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<TokenResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Kullanıcıyı bul
        var user = await _userManager.FindByEmailAsync(request.Email)
            ?? throw AuthException.InvalidCredentials();

        // 2. Şifreyi doğrula
        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
            throw AuthException.InvalidCredentials();

        // 3. Rolleri getir
        var roles = await _userManager.GetRolesAsync(user);

        // 4. Token üret
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        // 5. Refresh token'ı kaydet
        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15)
        );
    }
}
