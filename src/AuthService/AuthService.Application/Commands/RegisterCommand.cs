using AuthService.Application.DTOs;
using MediatR;

namespace AuthService.Application.Commands;

/// Yeni kullanıcı kayıt komutu.

public record RegisterCommand(
    string FullName,
    string Email,
    string Password,
    string Role = "User"
) : IRequest<TokenResponse>;
