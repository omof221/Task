using AuthService.Application.DTOs;
using MediatR;

namespace AuthService.Application.Commands;

/// <summary>
/// Yeni kullanıcı kayıt komutu.
/// CQRS — Command tarafı: kullanıcı ve token oluşturma yan etkisi üretir.
///
/// SRP: Yalnızca kayıt isteği verisini taşır.
/// </summary>
public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string Role = "User"
) : IRequest<TokenResponse>;
