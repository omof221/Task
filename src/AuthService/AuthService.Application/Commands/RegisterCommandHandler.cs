using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace AuthService.Application.Commands;


/// RegisterCommand'ı işleyen MediatR handler.

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, TokenResponse>
{
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public RegisterCommandHandler(
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ITokenService tokenService,
        IRefreshTokenRepository refreshTokenRepository)
    {
        _userManager = userManager;
        _roleManager = roleManager;
        _tokenService = tokenService;
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<TokenResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. E-posta çakışması kontrolü
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser is not null)
            throw AuthException.EmailAlreadyExists(request.Email);

        // 2. Kullanıcı oluştur
        var user = new AppUser
        {
            FullName = request.FullName,
            Email = request.Email,
            UserName = request.Email,
            Role = request.Role
        };

        // 3. Identity ile kaydet
        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw AuthException.RegistrationFailed(result.Errors.Select(e => e.Description));

        // 4. Rolü oluştur  ve kullanıcıya ata
        if (!await _roleManager.RoleExistsAsync(request.Role))
            await _roleManager.CreateAsync(new IdentityRole(request.Role));

        await _userManager.AddToRoleAsync(user, request.Role);

        // 5. Token üret
        var roles = await _userManager.GetRolesAsync(user);
        var accessToken = _tokenService.GenerateAccessToken(user, roles);
        var refreshToken = _tokenService.GenerateRefreshToken(user.Id);

        await _refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
        await _refreshTokenRepository.SaveChangesAsync(cancellationToken);

        return new TokenResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken.Token,
            AccessTokenExpiry: DateTime.UtcNow.AddMinutes(15)
        );
    }
}
