using CoffeeMachine.Domain;
using CoffeeMachine.Infrastructure.Persistence;
using FluentAssertions;

namespace CoffeeMachine.Tests;

public sealed class MachineSeedTests
{
    [Fact]
    public void Seed_Should_Create_ConfiguredMachine()
    {
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);

        machine.Status.Should().Be(MachineStatus.Off);
        machine.Products.Should().NotBeEmpty();
        machine.Recipes.Should().NotBeEmpty();
        machine.Ingredients.Should().HaveCountGreaterThan(1);
        machine.WaterTank.CurrentLevelMl.Should().BeGreaterThan(0);
    }
}
