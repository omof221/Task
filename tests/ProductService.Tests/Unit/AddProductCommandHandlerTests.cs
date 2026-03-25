using FluentAssertions;
using Moq;
using ProductService.Application;
using ProductService.Application.Commands;
using ProductService.Application.Interfaces;
using ProductService.Domain.Entities;

namespace ProductService.Tests.Unit;

/// <summary>
/// AddProductCommandHandler unit testleri.
/// Tüm bağımlılıklar Moq ile izole edilmiştir (SRP / DIP doğrulaması).
/// </summary>
public class AddProductCommandHandlerTests
{
    private readonly Mock<IProductRepository> _repoMock;
    private readonly Mock<IEventPublisher>    _publisherMock;
    private readonly Mock<ICacheService>      _cacheMock;
    private readonly AddProductCommandHandler _handler;

    public AddProductCommandHandlerTests()
    {
        _repoMock      = new Mock<IProductRepository>();
        _publisherMock = new Mock<IEventPublisher>();
        _cacheMock     = new Mock<ICacheService>();
        _handler       = new AddProductCommandHandler(
            _repoMock.Object, _publisherMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsProductDto()
    {
        // Arrange
        var command = new AddProductCommand("Test Ürün", "Açıklama", 99.99m, 10);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Test Ürün");
        result.Price.Should().Be(99.99m);
        result.Stock.Should().Be(10);
        result.Id.Should().NotBeEmpty();
        result.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ValidCommand_CallsRepositoryAddAndSave()
    {
        // Arrange
        var command = new AddProductCommand("Ürün", "Açıklama", 50m, 5);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — DB'ye yazma doğrulanır
        _repoMock.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_PublishesProductAddedEvent()
    {
        // Arrange
        var command = new AddProductCommand("Event Test", "Açıklama", 10m, 1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — event yayımlanır (event-driven mimarisi doğrulaması)
        _publisherMock.Verify(p =>
            p.PublishAsync(
                It.Is<ProductService.Domain.Events.ProductAddedEvent>(e =>
                    e.ProductName == "Event Test" && e.Price == 10m),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ValidCommand_InvalidatesCacheProductList()
    {
        // Arrange
        var command = new AddProductCommand("Cache Test", "Açıklama", 25m, 3);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — cache invalidation doğrulanır
        _cacheMock.Verify(c =>
            c.RemoveAsync(CacheKeys.ProductList, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_EmptyName_ThrowsArgumentException()
    {
        // Arrange — domain validation: boş isim geçersiz
        var command = new AddProductCommand("", "Açıklama", 10m, 5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_NegativePrice_ThrowsArgumentException()
    {
        // Arrange
        var command = new AddProductCommand("Ürün", "Açıklama", -1m, 5);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _handler.Handle(command, CancellationToken.None));
    }
}
