using FluentAssertions;
using Moq;
using ProductService.Application;
using ProductService.Application.DTOs;
using ProductService.Application.Interfaces;
using ProductService.Application.Queries;
using ProductService.Domain.Entities;

namespace ProductService.Tests.Unit;

/// <summary>
/// GetProductsQueryHandler unit testleri — Redis mock ile cache-aside doğrulaması.
/// </summary>
public class GetProductsQueryHandlerTests
{
    private readonly Mock<IProductRepository> _repoMock;
    private readonly Mock<ICacheService>      _cacheMock;
    private readonly GetProductsQueryHandler  _handler;

    public GetProductsQueryHandlerTests()
    {
        _repoMock  = new Mock<IProductRepository>();
        _cacheMock = new Mock<ICacheService>();
        _handler   = new GetProductsQueryHandler(_repoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task Handle_CacheHit_ReturnsCachedDataWithoutDbCall()
    {
        // Arrange — cache'te veri var
        var cachedList = new List<ProductDto>
        {
            new(Guid.NewGuid(), "Cached Ürün", "Açıklama", 100m, 5, true, DateTime.UtcNow, null)
        };

        _cacheMock
            .Setup(c => c.GetAsync<List<ProductDto>>(CacheKeys.ProductList, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedList);

        // Act
        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert — DB'ye gidilmez (cache hit)
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Cached Ürün");
        _repoMock.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CacheMiss_FetchesFromDbAndPopulatesCache()
    {
        // Arrange — cache boş, DB'de veri var
        _cacheMock
            .Setup(c => c.GetAsync<List<ProductDto>>(CacheKeys.ProductList, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ProductDto>?)null);

        var dbProducts = new List<Product>
        {
            Product.Create("DB Ürün", "Açıklama", 50m, 3)
        };

        _repoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbProducts);

        // Act
        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert — DB'den yüklendi ve cache'e yazıldı
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("DB Ürün");

        _repoMock.Verify(r => r.GetActiveAsync(It.IsAny<CancellationToken>()), Times.Once);
        _cacheMock.Verify(c =>
            c.SetAsync(
                CacheKeys.ProductList,
                It.IsAny<List<ProductDto>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CacheMiss_EmptyDb_ReturnsEmptyList()
    {
        // Arrange
        _cacheMock
            .Setup(c => c.GetAsync<List<ProductDto>>(CacheKeys.ProductList, It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<ProductDto>?)null);

        _repoMock
            .Setup(r => r.GetActiveAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        var result = await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CacheHit_DoesNotWriteToCache()
    {
        // Arrange — cache zaten dolu
        var cachedList = new List<ProductDto>
        {
            new(Guid.NewGuid(), "Ürün", "Açıklama", 10m, 1, true, DateTime.UtcNow, null)
        };

        _cacheMock
            .Setup(c => c.GetAsync<List<ProductDto>>(CacheKeys.ProductList, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedList);

        // Act
        await _handler.Handle(new GetProductsQuery(), CancellationToken.None);

        // Assert — cache'e tekrar yazılmaz
        _cacheMock.Verify(c =>
            c.SetAsync(
                It.IsAny<string>(),
                It.IsAny<List<ProductDto>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
