using CoffeeMachine.Domain;
using CoffeeMachine.Infrastructure.Simulation;
using CoffeeMachine.Tests.TestDoubles;
using FluentAssertions;

namespace CoffeeMachine.Tests.Infrastructure;

public sealed class SimulationTests
{
    [Fact]
    public async Task SimulationTick_Should_Advance_Order_To_WaitingForHeat_Then_Dispensing()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var clock = new FakeClock();
        var eventBus = new FakeEventBus();
        var simulation = new SimulationBackgroundService(repository, eventBus, new FakeRandomProvider(0.99, 0.99, 0.99), clock);

        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Status = MachineStatus.Ready;
            machine.Credit.CurrentCredit = 5m;
            machine.Boiler.CurrentTemperature = 20;
            machine.Boiler.TargetTemperature = 91;
            machine.DispensingUnit.IsBusy = true;
            machine.DispensingUnit.CurrentOrderId = "ord-1";
            machine.Orders.Insert(0, new Order
            {
                Id = "ord-1",
                ProductId = "espresso",
                ProductName = "Espresso",
                RecipeId = "espresso-recipe",
                Status = OrderStatus.Validating,
                CreatedAtUtc = clock.UtcNow
            });
            return 0;
        });

        await simulation.RunSingleTickAsync();
        var firstStatus = await repository.ReadAsync(machine => machine.Orders.First().Status);

        firstStatus.Should().Be(OrderStatus.WaitingForHeat);

        await repository.WriteAsync(machine =>
        {
            machine.Boiler.CurrentTemperature = 91;
            return 0;
        });

        await simulation.RunSingleTickAsync();
        var secondStatus = await repository.ReadAsync(machine => machine.Orders.First().Status);

        secondStatus.Should().Be(OrderStatus.DispensingIngredient);
    }

    [Fact]
    public async Task SimulationTick_Should_Fail_Order_When_Random_Fault_Happens()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var clock = new FakeClock();
        var eventBus = new FakeEventBus();
        var simulation = new SimulationBackgroundService(repository, eventBus, new FakeRandomProvider(0.0), clock);

        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Boiler.CurrentTemperature = 95;
            machine.Boiler.TargetTemperature = 91;
            machine.DispensingUnit.IsBusy = true;
            machine.DispensingUnit.CurrentOrderId = "ord-2";
            machine.Orders.Insert(0, new Order
            {
                Id = "ord-2",
                ProductId = "espresso",
                ProductName = "Espresso",
                RecipeId = "espresso-recipe",
                Status = OrderStatus.DispensingIngredient,
                CreatedAtUtc = clock.UtcNow
            });
            return 0;
        });

        await simulation.RunSingleTickAsync();
        var order = await repository.ReadAsync(machine => machine.Orders.First());

        order.Status.Should().Be(OrderStatus.Failed);
        eventBus.Published.Select(x => x.EventName).Should().Contain("ErrorRaised");
    }

    [Fact]
    public async Task SimulationTick_Should_Trigger_Low_Water_Warning()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var simulation = new SimulationBackgroundService(repository, new FakeEventBus(), new FakeRandomProvider(), new FakeClock());

        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.WaterTank.CurrentLevelMl = machine.WaterTank.LowLevelThresholdMl;
            return 0;
        });

        await simulation.RunSingleTickAsync();
        var diagnostics = await repository.ReadAsync(machine => machine.Diagnostics);

        diagnostics.Should().Contain(x => x.Code == "WaterLow" && x.Active);
    }
}
