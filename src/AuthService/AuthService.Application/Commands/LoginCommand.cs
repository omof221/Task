using AuthService.Application.DTOs;
using MediatR;

namespace AuthService.Application.Commands;

public record LoginCommand(string Email, string Password) : IRequest<TokenResponse>;
