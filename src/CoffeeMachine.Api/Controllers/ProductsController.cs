using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController(ICatalogApplicationService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.Product>> GetAll(CancellationToken cancellationToken) =>
        service.GetProductsAsync(cancellationToken);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var product = await service.GetProductAsync(id, cancellationToken);
        return product is null ? NotFound() : Ok(product);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductUpsertRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.CreateProductAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductUpsertRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.UpdateProductAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        this.FromResult(await service.DeleteProductAsync(id, cancellationToken));
}
