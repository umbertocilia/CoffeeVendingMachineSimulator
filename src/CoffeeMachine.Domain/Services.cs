namespace CoffeeMachine.Domain;

public static class MachineStateRules
{
    public static bool CanPowerOn(CoffeeMachine machine) => !machine.PowerFaultActive;

    public static bool CanStartOrder(CoffeeMachine machine) =>
        machine.PowerOn &&
        machine.Status is not MachineStatus.Error &&
        machine.Status is not MachineStatus.OutOfService &&
        !machine.Maintenance.MaintenanceRequired &&
        !machine.DispensingUnit.IsBusy;
}

public static class RecipeValidator
{
    public static (bool IsValid, string? Error) Validate(Recipe recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.Name))
        {
            return (false, "Recipe name is required.");
        }

        if (recipe.TargetTemperature <= 0)
        {
            return (false, "Target temperature must be greater than zero.");
        }

        if (recipe.Steps.Count == 0)
        {
            return (false, "At least one recipe step is required.");
        }

        if (recipe.Steps.Any(step => step.Quantity <= 0 || step.DurationMs <= 0 || string.IsNullOrWhiteSpace(step.IngredientKey)))
        {
            return (false, "Each recipe step must have ingredient, quantity and duration.");
        }

        return (true, null);
    }
}

public static class ProductAvailabilityEvaluator
{
    public static (bool Available, string Reason) Evaluate(CoffeeMachine machine, Product product, Recipe? recipe)
    {
        if (!product.Enabled)
        {
            return (false, "Product disabled.");
        }

        if (recipe is null)
        {
            return (false, "Recipe missing.");
        }

        if (!MachineStateRules.CanStartOrder(machine))
        {
            return (false, "Machine not available.");
        }

        foreach (var step in recipe.Steps)
        {
            if (step.IngredientKey.Equals("water", StringComparison.OrdinalIgnoreCase))
            {
                if (machine.WaterTank.CurrentLevelMl < step.Quantity)
                {
                    return (false, "Insufficient water.");
                }

                continue;
            }

            var ingredient = machine.Ingredients.FirstOrDefault(x => x.Name.Equals(step.IngredientKey, StringComparison.OrdinalIgnoreCase));
            if (ingredient is null || !ingredient.Enabled || ingredient.CurrentLevel < step.Quantity)
            {
                return (false, $"Insufficient ingredient: {step.IngredientKey}.");
            }
        }

        return (true, string.Empty);
    }
}

public static class MachineResourceCalculator
{
    public static void ConsumeRecipe(CoffeeMachine machine, Recipe recipe)
    {
        foreach (var step in recipe.Steps)
        {
            if (step.IngredientKey.Equals("water", StringComparison.OrdinalIgnoreCase))
            {
                machine.WaterTank.CurrentLevelMl -= step.Quantity;
                continue;
            }

            var ingredient = machine.Ingredients.First(x => x.Name.Equals(step.IngredientKey, StringComparison.OrdinalIgnoreCase));
            ingredient.CurrentLevel -= step.Quantity;
        }
    }
}

public static class ResourceWarningEvaluator
{
    public static IReadOnlyCollection<string> GetWarnings(CoffeeMachine machine)
    {
        var warnings = new List<string>();

        if (machine.WaterTank.CurrentLevelMl <= machine.WaterTank.LowLevelThresholdMl)
        {
            warnings.Add("WaterLow");
        }

        warnings.AddRange(machine.Ingredients
            .Where(x => x.CurrentLevel <= x.LowLevelThreshold)
            .Select(x => $"Low-{x.Id}"));

        return warnings;
    }
}

public static class MaintenancePolicy
{
    public static bool IsMaintenanceRequired(int dispenseCount, int threshold) => dispenseCount >= threshold;
}
