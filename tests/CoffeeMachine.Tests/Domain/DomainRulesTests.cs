using CoffeeMachine.Domain;
using CoffeeMachine.Infrastructure.Persistence;
using FluentAssertions;

namespace CoffeeMachine.Tests.Domain;

public sealed class DomainRulesTests
{
    [Fact]
    public void MachineStateRules_Should_Reject_PowerOn_When_PowerFault_IsActive()
    {
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);
        machine.PowerFaultActive = true;

        MachineStateRules.CanPowerOn(machine).Should().BeFalse();
    }

    [Fact]
    public void ProductAvailability_Should_BeFalse_When_Ingredient_IsMissing()
    {
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);
        machine.PowerOn = true;
        machine.Status = MachineStatus.Ready;
        machine.Ingredients.First(x => x.Name == "Coffee").CurrentLevel = 0;

        var product = machine.Products.First(x => x.Id == "espresso");
        var recipe = machine.Recipes.First(x => x.Id == product.RecipeId);

        var result = ProductAvailabilityEvaluator.Evaluate(machine, product, recipe);

        result.Available.Should().BeFalse();
        result.Reason.Should().Contain("Insufficient ingredient");
    }

    [Fact]
    public void ResourceCalculator_Should_Consume_Recipe_Resources()
    {
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);
        var recipe = machine.Recipes.First(x => x.Id == "espresso-recipe");
        var initialWater = machine.WaterTank.CurrentLevelMl;
        var initialCoffee = machine.Ingredients.First(x => x.Name == "Coffee").CurrentLevel;

        MachineResourceCalculator.ConsumeRecipe(machine, recipe);

        machine.WaterTank.CurrentLevelMl.Should().Be(initialWater - 40);
        machine.Ingredients.First(x => x.Name == "Coffee").CurrentLevel.Should().Be(initialCoffee - 9);
    }

    [Fact]
    public void WarningEvaluator_Should_Report_LowWater()
    {
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);
        machine.WaterTank.CurrentLevelMl = machine.WaterTank.LowLevelThresholdMl;

        var warnings = ResourceWarningEvaluator.GetWarnings(machine);

        warnings.Should().Contain("WaterLow");
    }

    [Fact]
    public void MaintenancePolicy_Should_Require_Maintenance_After_Threshold()
    {
        MaintenancePolicy.IsMaintenanceRequired(50, 50).Should().BeTrue();
        MaintenancePolicy.IsMaintenanceRequired(49, 50).Should().BeFalse();
    }

    [Fact]
    public void Boiler_Should_Allow_Target_Configuration()
    {
        var boiler = new Boiler { CurrentTemperature = 80, TargetTemperature = 92 };

        boiler.CurrentTemperature.Should().BeLessThan(boiler.TargetTemperature);
    }

    [Fact]
    public void RecipeValidator_Should_Reject_Invalid_Recipe()
    {
        var recipe = new Recipe
        {
            Name = "Broken",
            TargetTemperature = 0,
            Steps = []
        };

        var validation = RecipeValidator.Validate(recipe);

        validation.IsValid.Should().BeFalse();
        validation.Error.Should().NotBeNullOrWhiteSpace();
    }
}
