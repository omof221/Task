using AuthService.Application.DTOs;
using MediatR;

namespace AuthService.Application.Commands;

/// <summary>
/// Mevcut refresh token kullanarak yeni access token elde etme komutu.
/// CQRS — Command: eski token revoke edilip yeni token yazılır (yan etki).
///
/// SRP: Yalnızca token yenileme verisini taşır.
/// </summary>
public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponse>;
