using AuthService.Application.Commands;
using AuthService.Application.Interfaces;
using AuthService.Domain.Entities;
using AuthService.Domain.Exceptions;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace AuthService.Tests.Unit;

/// <summary>
/// LoginCommandHandler unit testleri.
/// Tüm bağımlılıklar Moq ile izole edilir — DB bağlantısı yoktur.
///
/// Test senaryoları:
/// 1) Geçerli kimlik bilgileri → TokenResponse döner
/// 2) Kullanıcı bulunamazsa → AuthException fırlatır
/// 3) Şifre yanlışsa → AuthException fırlatır
/// </summary>
public class LoginCommandHandlerTests
{
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<ITokenService> _tokenServiceMock;
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        // UserManager mock'u — constructor parametreli olduğu için özel kurulum gerekir
        var store = new Mock<IUserStore<AppUser>>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

        _tokenServiceMock = new Mock<ITokenService>();
        _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();

        _handler = new LoginCommandHandler(
            _userManagerMock.Object,
            _tokenServiceMock.Object,
            _refreshTokenRepositoryMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldReturnTokenResponse()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@test.com",
            UserName = "user@test.com",
            FullName = "Test User"
        };

        var command = new LoginCommand("user@test.com", "ValidPass123!");

        _userManagerMock
            .Setup(um => um.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(um => um.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(true);

        _userManagerMock
            .Setup(um => um.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "User" });

        _tokenServiceMock
            .Setup(ts => ts.GenerateAccessToken(user, It.IsAny<IList<string>>()))
            .Returns("mocked.access.token");

        _tokenServiceMock
            .Setup(ts => ts.GenerateRefreshToken(user.Id))
            .Returns(new RefreshToken
            {
                Token = "mocked-refresh-token",
                UserId = user.Id,
                ExpiryDate = DateTime.UtcNow.AddDays(7)
            });

        _refreshTokenRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _refreshTokenRepositoryMock
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.AccessToken.Should().Be("mocked.access.token");
        result.RefreshToken.Should().Be("mocked-refresh-token");
        result.TokenType.Should().Be("Bearer");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldThrowAuthException()
    {
        // Arrange
        var command = new LoginCommand("notexist@test.com", "AnyPass123!");

        _userManagerMock
            .Setup(um => um.FindByEmailAsync(command.Email))
            .ReturnsAsync((AppUser?)null);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthException>()
            .Where(ex => ex.StatusCode == 401);
    }

    [Fact]
    public async Task Handle_WithWrongPassword_ShouldThrowAuthException()
    {
        // Arrange
        var user = new AppUser
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@test.com"
        };

        var command = new LoginCommand("user@test.com", "WrongPassword!");

        _userManagerMock
            .Setup(um => um.FindByEmailAsync(command.Email))
            .ReturnsAsync(user);

        _userManagerMock
            .Setup(um => um.CheckPasswordAsync(user, command.Password))
            .ReturnsAsync(false);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<AuthException>()
            .Where(ex => ex.StatusCode == 401);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_ShouldCallSaveChanges()
    {
        // Arrange
        var user = new AppUser { Id = Guid.NewGuid().ToString(), Email = "user@test.com" };
        var command = new LoginCommand("user@test.com", "ValidPass123!");

        _userManagerMock.Setup(um => um.FindByEmailAsync(command.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(um => um.CheckPasswordAsync(user, command.Password)).ReturnsAsync(true);
        _userManagerMock.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(new List<string>());
        _tokenServiceMock.Setup(ts => ts.GenerateAccessToken(user, It.IsAny<IList<string>>())).Returns("token");
        _tokenServiceMock.Setup(ts => ts.GenerateRefreshToken(user.Id)).Returns(new RefreshToken { Token = "rt" });
        _refreshTokenRepositoryMock.Setup(r => r.AddAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _refreshTokenRepositoryMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — SaveChanges çağrıldı mı?
        _refreshTokenRepositoryMock.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
