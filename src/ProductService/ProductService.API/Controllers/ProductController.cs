using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Application.Commands;
using ProductService.Application.DTOs;
using ProductService.Application.Queries;

namespace ProductService.API.Controllers;

/// <summary>
/// Ürün CRUD endpoint'leri.
/// SRP: Yalnızca HTTP binding ve MediatR dispatch.
/// CQRS: Command (write) ve Query (read) ayrıştırılmış.
/// </summary>
[ApiController]
[Route("api/products")]
[Produces("application/json")]
public class ProductController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProductController(IMediator mediator) => _mediator = mediator;

    /// <summary>Tüm aktif ürünleri listeler — Redis cache-aside.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductsQuery(), ct);
        return Ok(result);
    }

    /// <summary>ID'ye göre tek ürün getirir — Redis cache-aside.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>
    /// Yeni ürün ekler.
    /// Event: ProductAddedEvent → RabbitMQ/Kafka → LogService.
    /// Cache Invalidation: ProductList cache silinir.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Add([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var command = new AddProductCommand(
            request.Name, request.Description, request.Price, request.Stock);

        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Ürün günceller — JWT [Authorize] gerektirir.
    /// Event: ProductUpdatedEvent → RabbitMQ/Kafka → LogService.
    /// Cache Invalidation: Hem liste hem tekil cache silinir.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken ct)
    {
        var command = new UpdateProductCommand(
            id, request.Name, request.Description, request.Price, request.Stock);

        var result = await _mediator.Send(command, ct);
        return Ok(result);
    }
}
