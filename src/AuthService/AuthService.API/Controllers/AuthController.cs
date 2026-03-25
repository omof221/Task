using AuthService.Application.Commands;
using AuthService.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.API.Controllers;

/// <summary>
/// Kimlik doğrulama endpoint'leri.
///
/// SRP: Controller yalnızca HTTP iletişimini yönetir;
/// iş mantığı MediatR komutları aracılığıyla Application katmanına devredilir.
///
/// Endpoint'ler:
///   POST /api/auth/register  — Yeni kullanıcı kaydı
///   POST /api/auth/login     — Giriş → JWT + Refresh Token
///   POST /api/auth/refresh   — Access token yenileme
///   GET  /api/auth/me        — Mevcut kullanıcı bilgisi (test endpoint)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Yeni kullanıcı kaydeder ve JWT token döner.
    /// </summary>
    /// <param name="request">Kayıt bilgileri (FullName, Email, Password, Role).</param>
    /// <returns>Access token ve refresh token.</returns>
    [HttpPost("register")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var command = new RegisterCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.Role
        );

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(Me), null, result);
    }

    /// <summary>
    /// Kullanıcı girişi yapar ve JWT token döner.
    /// </summary>
    /// <param name="request">Giriş bilgileri (Email, Password).</param>
    /// <returns>Access token ve refresh token.</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var command = new LoginCommand(request.Email, request.Password);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Refresh token kullanarak yeni access token üretir (Token Rotation).
    /// </summary>
    /// <param name="refreshToken">Mevcut refresh token değeri.</param>
    /// <returns>Yeni access token ve refresh token.</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(TokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var command = new RefreshTokenCommand(refreshToken);
        var result = await _mediator.Send(command);
        return Ok(result);
    }

    /// <summary>
    /// Geçerli JWT ile mevcut kullanıcı claim'lerini döner (test endpoint).
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                  ?? User.FindFirst("sub")?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var roles = User.FindAll(System.Security.Claims.ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToList();

        return Ok(new { UserId = userId, Email = email, Roles = roles });
    }
}
