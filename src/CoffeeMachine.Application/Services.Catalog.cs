using CoffeeMachine.Application.Results;
using CoffeeMachine.Domain;

namespace CoffeeMachine.Application;

public sealed class CatalogApplicationService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IStateSnapshotService snapshotService,
    IClock clock) : ICatalogApplicationService
{
    public Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<Product>)machine.Products.Select(x => x.Clone()).ToList(), cancellationToken);

    public Task<Product?> GetProductAsync(string id, CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.Products.FirstOrDefault(x => x.Id == id)?.Clone(), cancellationToken);

    public async Task<Result<Product>> CreateProductAsync(ProductUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            if (machine.Products.Any(x => x.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            {
                return Result<Product>.Failure("Product with the same name already exists.");
            }

            var product = new Product
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = request.Name,
                Price = request.Price,
                RecipeId = request.RecipeId,
                Enabled = request.Enabled
            };
            machine.Products.Add(product);
            machine.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "catalog", Message = $"Product created: {product.Name}" });
            return Result<Product>.Success(product.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ProductAvailabilityChanged", result.Value!, cancellationToken);
            await snapshotService.SaveSnapshotAsync("product-create", cancellationToken);
        }

        return result;
    }

    public async Task<Result<Product>> UpdateProductAsync(string id, ProductUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            var product = machine.Products.FirstOrDefault(x => x.Id == id);
            if (product is null)
            {
                return Result<Product>.Failure("Product not found.");
            }

            product.Name = request.Name;
            product.Price = request.Price;
            product.RecipeId = request.RecipeId;
            product.Enabled = request.Enabled;
            return Result<Product>.Success(product.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ProductAvailabilityChanged", result.Value!, cancellationToken);
            await snapshotService.SaveSnapshotAsync("product-update", cancellationToken);
        }

        return result;
    }

    public async Task<Result> DeleteProductAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            var removed = machine.Products.RemoveAll(x => x.Id == id);
            return removed == 0 ? Result.Failure("Product not found.") : Result.Success();
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ProductAvailabilityChanged", new { productId = id, deleted = true }, cancellationToken);
            await snapshotService.SaveSnapshotAsync("product-delete", cancellationToken);
        }

        return result;
    }

    public Task<IReadOnlyCollection<Recipe>> GetRecipesAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<Recipe>)machine.Recipes.Select(x => x.Clone()).ToList(), cancellationToken);

    public Task<Recipe?> GetRecipeAsync(string id, CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.Recipes.FirstOrDefault(x => x.Id == id)?.Clone(), cancellationToken);

    public async Task<Result<Recipe>> CreateRecipeAsync(RecipeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var recipe = MapRecipe(Guid.NewGuid().ToString("N"), request);
        var result = await repository.WriteAsync(machine =>
        {
            machine.Recipes.Add(recipe);
            return Result<Recipe>.Success(recipe.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ConfigurationChanged", new { recipeId = recipe.Id }, cancellationToken);
            await snapshotService.SaveSnapshotAsync("recipe-create", cancellationToken);
        }

        return result;
    }

    public async Task<Result<Recipe>> UpdateRecipeAsync(string id, RecipeUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            var recipe = machine.Recipes.FirstOrDefault(x => x.Id == id);
            if (recipe is null)
            {
                return Result<Recipe>.Failure("Recipe not found.");
            }

            recipe.Name = request.Name;
            recipe.TargetTemperature = request.TargetTemperature;
            recipe.Steps = request.Steps.Select(step => new RecipeStep
            {
                Sequence = step.Sequence,
                IngredientKey = step.IngredientKey,
                Quantity = step.Quantity,
                Unit = step.Unit,
                DurationMs = step.DurationMs
            }).OrderBy(x => x.Sequence).ToList();
            return Result<Recipe>.Success(recipe.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ConfigurationChanged", new { recipeId = id }, cancellationToken);
            await snapshotService.SaveSnapshotAsync("recipe-update", cancellationToken);
        }

        return result;
    }

    public async Task<Result> DeleteRecipeAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            if (machine.Products.Any(x => x.RecipeId == id))
            {
                return Result.Failure("Recipe is used by at least one product.");
            }

            var removed = machine.Recipes.RemoveAll(x => x.Id == id);
            return removed == 0 ? Result.Failure("Recipe not found.") : Result.Success();
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ConfigurationChanged", new { recipeId = id, deleted = true }, cancellationToken);
            await snapshotService.SaveSnapshotAsync("recipe-delete", cancellationToken);
        }

        return result;
    }

    public Task<IReadOnlyCollection<IngredientContainer>> GetIngredientsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<IngredientContainer>)machine.Ingredients.Select(x => x.Clone()).ToList(), cancellationToken);

    public Task<IngredientContainer?> GetIngredientAsync(string id, CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.Ingredients.FirstOrDefault(x => x.Id == id)?.Clone(), cancellationToken);

    public async Task<Result<IngredientContainer>> UpdateIngredientAsync(string id, IngredientUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            var ingredient = machine.Ingredients.FirstOrDefault(x => x.Id == id);
            if (ingredient is null)
            {
                return Result<IngredientContainer>.Failure("Ingredient not found.");
            }

            ingredient.Name = request.Name;
            ingredient.Unit = request.Unit;
            ingredient.Capacity = request.Capacity;
            ingredient.CurrentLevel = Math.Clamp(request.CurrentLevel, 0, request.Capacity);
            ingredient.LowLevelThreshold = request.LowLevelThreshold;
            ingredient.Enabled = request.Enabled;
            return Result<IngredientContainer>.Success(ingredient.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("IngredientLevelChanged", result.Value!, cancellationToken);
            await snapshotService.SaveSnapshotAsync("ingredient-update", cancellationToken);
        }

        return result;
    }

    public async Task<Result<IngredientContainer>> RefillIngredientAsync(string id, RefillIngredientRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            return Result<IngredientContainer>.Failure("Quantity must be greater than zero.");
        }

        var result = await repository.WriteAsync(machine =>
        {
            var ingredient = machine.Ingredients.FirstOrDefault(x => x.Id == id);
            if (ingredient is null)
            {
                return Result<IngredientContainer>.Failure("Ingredient not found.");
            }

            ingredient.CurrentLevel = Math.Min(ingredient.Capacity, ingredient.CurrentLevel + request.Quantity);
            machine.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "ingredient", Message = $"Refill {ingredient.Name}: {request.Quantity}" });
            return Result<IngredientContainer>.Success(ingredient.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("IngredientLevelChanged", result.Value!, cancellationToken);
            await snapshotService.SaveSnapshotAsync("ingredient-refill", cancellationToken);
        }

        return result;
    }

    public Task<WaterTank> GetWaterTankAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.WaterTank.Clone(), cancellationToken);

    public async Task<Result<WaterTank>> RefillWaterAsync(double quantityMl, CancellationToken cancellationToken = default)
    {
        if (quantityMl <= 0)
        {
            return Result<WaterTank>.Failure("Quantity must be greater than zero.");
        }

        var result = await repository.WriteAsync(machine =>
        {
            machine.WaterTank.CurrentLevelMl = Math.Min(machine.WaterTank.CapacityMl, machine.WaterTank.CurrentLevelMl + quantityMl);
            machine.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "water", Message = $"Water refill: {quantityMl} ml" });
            return Result<WaterTank>.Success(machine.WaterTank.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("IngredientLevelChanged", new { ingredientId = "water-main", level = result.Value!.CurrentLevelMl }, cancellationToken);
            await snapshotService.SaveSnapshotAsync("water-refill", cancellationToken);
        }

        return result;
    }

    private static Recipe MapRecipe(string id, RecipeUpsertRequest request) => new()
    {
        Id = id,
        Name = request.Name,
        TargetTemperature = request.TargetTemperature,
        Steps = request.Steps
            .Select(step => new RecipeStep
            {
                Sequence = step.Sequence,
                IngredientKey = step.IngredientKey,
                Quantity = step.Quantity,
                Unit = step.Unit,
                DurationMs = step.DurationMs
            })
            .OrderBy(x => x.Sequence)
            .ToList()
    };
}
