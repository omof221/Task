using AuthService.Application.DTOs;
using MediatR;

namespace AuthService.Application.Commands;

/// <summary>
/// Kullanıcı giriş komutu.
/// CQRS — Command tarafı: yan etki üretir (refresh token kaydedilir).
///
/// SRP: Bu sınıf yalnızca giriş isteğinin verisini taşır.
/// Giriş mantığı LoginCommandHandler'dadır.
/// </summary>
public record LoginCommand(string Email, string Password) : IRequest<TokenResponse>;
