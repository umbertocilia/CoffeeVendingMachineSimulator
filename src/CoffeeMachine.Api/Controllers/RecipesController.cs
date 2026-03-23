using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api/recipes")]
public sealed class RecipesController(ICatalogApplicationService service) : ControllerBase
{
    [HttpGet]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.Recipe>> GetAll(CancellationToken cancellationToken) =>
        service.GetRecipesAsync(cancellationToken);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var recipe = await service.GetRecipeAsync(id, cancellationToken);
        return recipe is null ? NotFound() : Ok(recipe);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] RecipeUpsertRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.CreateRecipeAsync(request, cancellationToken));

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] RecipeUpsertRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.UpdateRecipeAsync(id, request, cancellationToken));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken) =>
        this.FromResult(await service.DeleteRecipeAsync(id, cancellationToken));
}
