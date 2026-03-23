using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class IngredientsController(ICatalogApplicationService service) : ControllerBase
{
    [HttpGet("ingredients")]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.IngredientContainer>> GetIngredients(CancellationToken cancellationToken) =>
        service.GetIngredientsAsync(cancellationToken);

    [HttpGet("ingredients/{id}")]
    public async Task<IActionResult> GetIngredient(string id, CancellationToken cancellationToken)
    {
        var ingredient = await service.GetIngredientAsync(id, cancellationToken);
        return ingredient is null ? NotFound() : Ok(ingredient);
    }

    [HttpPut("ingredients/{id}")]
    public async Task<IActionResult> UpdateIngredient(string id, [FromBody] IngredientUpdateRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.UpdateIngredientAsync(id, request, cancellationToken));

    [HttpPost("ingredients/{id}/refill")]
    public async Task<IActionResult> RefillIngredient(string id, [FromBody] RefillIngredientRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.RefillIngredientAsync(id, request, cancellationToken));

    [HttpGet("tanks")]
    public Task<CoffeeMachine.Domain.WaterTank> GetTanks(CancellationToken cancellationToken) =>
        service.GetWaterTankAsync(cancellationToken);

    [HttpGet("tanks/status")]
    public Task<CoffeeMachine.Domain.WaterTank> GetTankStatus(CancellationToken cancellationToken) =>
        service.GetWaterTankAsync(cancellationToken);

    [HttpPost("water/refill")]
    public async Task<IActionResult> RefillWater([FromBody] RefillIngredientRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.RefillWaterAsync(request.Quantity, cancellationToken));
}
