using CoffeeMachine.Application;
using CoffeeMachine.Domain;
using CoffeeMachine.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using CoffeeMachine.Tests.TestDoubles;

namespace CoffeeMachine.Tests.Application;

public sealed class ApplicationServiceTests
{
    [Fact]
    public async Task CreateOrder_Should_Succeed_With_Sufficient_Credit()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var clock = new FakeClock();
        var eventBus = new FakeEventBus();
        var snapshotService = new NoOpSnapshotService();
        var service = new OrderApplicationService(repository, eventBus, snapshotService, clock);

        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Status = MachineStatus.Ready;
            machine.Credit.CurrentCredit = 2m;
            return 0;
        });

        var result = await service.CreateOrderAsync(new CreateOrderRequest("espresso"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Status.Should().Be(OrderStatus.Validating);
        eventBus.Published.Select(x => x.EventName).Should().Contain("OrderCreated");
    }

    [Fact]
    public async Task CreateOrder_Should_Update_Metrics_And_Charge_Credit()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = new OrderApplicationService(repository, new FakeEventBus(), new NoOpSnapshotService(), new FakeClock());

        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Status = MachineStatus.Ready;
            machine.Credit.CurrentCredit = 2m;
            return 0;
        });

        await service.CreateOrderAsync(new CreateOrderRequest("espresso"));
        var state = await repository.ReadAsync(machine => new { machine.Metrics.TotalOrders, machine.Credit.CurrentCredit });

        state.TotalOrders.Should().Be(1);
        state.CurrentCredit.Should().Be(1.5m);
    }

    [Fact]
    public async Task CreateOrder_Should_Fail_With_Insufficient_Credit()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = new OrderApplicationService(repository, new FakeEventBus(), new NoOpSnapshotService(), new FakeClock());
        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Status = MachineStatus.Ready;
            machine.Credit.CurrentCredit = 0.1m;
            return 0;
        });

        var result = await service.CreateOrderAsync(new CreateOrderRequest("espresso"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Insufficient credit.");
    }

    [Fact]
    public async Task CreateOrder_Should_Fail_When_Ingredient_Is_Insufficient()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = new OrderApplicationService(repository, new FakeEventBus(), new NoOpSnapshotService(), new FakeClock());
        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Status = MachineStatus.Ready;
            machine.Credit.CurrentCredit = 5m;
            machine.Ingredients.First(x => x.Name == "Coffee").CurrentLevel = 0;
            return 0;
        });

        var result = await service.CreateOrderAsync(new CreateOrderRequest("espresso"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Required resources are not available.");
    }

    [Fact]
    public async Task CreateOrder_Should_Fail_When_Machine_Is_Off()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = new OrderApplicationService(repository, new FakeEventBus(), new NoOpSnapshotService(), new FakeClock());

        var result = await service.CreateOrderAsync(new CreateOrderRequest("espresso"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Machine is powered off.");
    }

    [Fact]
    public async Task CreateOrder_Should_Fail_When_Maintenance_Is_Required()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = new OrderApplicationService(repository, new FakeEventBus(), new NoOpSnapshotService(), new FakeClock());
        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Status = MachineStatus.MaintenanceRequired;
            machine.Credit.CurrentCredit = 5m;
            machine.Maintenance.MaintenanceRequired = true;
            return 0;
        });

        var result = await service.CreateOrderAsync(new CreateOrderRequest("espresso"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Maintenance required.");
    }

    [Fact]
    public async Task DiagnosticsFaultInjection_Should_Register_Error()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var eventBus = new FakeEventBus();
        var service = new DiagnosticsApplicationService(repository, eventBus, new NoOpSnapshotService(), new NoOpLogReader(), new FakeClock());

        var result = await service.InjectFaultAsync(new FaultInjectionRequest(FaultType.HeatingFailure, "Injected"));
        var diagnostics = await service.GetActiveErrorsAsync();

        result.IsSuccess.Should().BeTrue();
        diagnostics.Should().ContainSingle(x => x.Code == "HeatingFailure");
        eventBus.Published.Select(x => x.EventName).Should().Contain("ErrorRaised");
    }

    [Fact]
    public async Task ResetMaintenance_Should_Clear_Maintenance_State()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = new MachineApplicationService(repository, new FakeEventBus(), new NoOpSnapshotService(), new FakeClock());
        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Maintenance.MaintenanceRequired = true;
            machine.Maintenance.WearPercentage = 75;
            machine.Maintenance.DispenseCount = 51;
            return 0;
        });

        var result = await service.ResetMaintenanceAsync();
        var status = await service.GetStatusAsync();

        result.IsSuccess.Should().BeTrue();
        status.MaintenanceRequired.Should().BeFalse();
    }

    [Fact]
    public async Task SaveSnapshot_Should_Write_And_Restore_Correctly()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var fileStorage = new FakeFileStorage();
        var serializer = new JsonStateSnapshotSerializer();
        var clock = new FakeClock();
        var eventBus = new FakeEventBus();
        var options = Options.Create(new PersistenceOptions { SnapshotPath = "state.json" });
        var service = new JsonStatePersistenceService(repository, fileStorage, serializer, clock, eventBus, NullLogger<JsonStatePersistenceService>.Instance, options);

        await repository.WriteAsync(machine =>
        {
            machine.PowerOn = true;
            machine.Credit.CurrentCredit = 3.5m;
            return 0;
        });

        await service.SaveAsync();
        await repository.WriteAsync(machine =>
        {
            machine.Credit.CurrentCredit = 0m;
            return 0;
        });
        var restore = await service.RestoreAsync();
        var restoredCredit = await repository.ReadAsync(machine => machine.Credit.CurrentCredit);

        restore.RestoredFromSnapshot.Should().BeTrue();
        restoredCredit.Should().Be(3.5m);
        eventBus.Published.Select(x => x.EventName).Should().Contain(["SnapshotSaved", "SnapshotRestored"]);
    }
}

file sealed class NoOpSnapshotService : IStateSnapshotService
{
    public Task SaveSnapshotAsync(string reason, CancellationToken cancellationToken = default) => Task.CompletedTask;
}

file sealed class NoOpLogReader : ILogReader
{
    public Task<IReadOnlyCollection<string>> ReadRecentAsync(int lines, CancellationToken cancellationToken = default) =>
        Task.FromResult((IReadOnlyCollection<string>)Array.Empty<string>());
}
