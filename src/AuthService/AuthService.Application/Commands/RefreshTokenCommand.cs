using AuthService.Application.DTOs;
using MediatR;

namespace AuthService.Application.Commands;


public record RefreshTokenCommand(string RefreshToken) : IRequest<TokenResponse>;
