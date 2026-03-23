using CoffeeMachine.Application.Results;
using CoffeeMachine.Domain;

namespace CoffeeMachine.Application;

public sealed class OrderApplicationService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IStateSnapshotService snapshotService,
    IClock clock) : IOrderApplicationService
{
    public async Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            if (!machine.PowerOn)
            {
                return Result<Order>.Failure("Machine is powered off.");
            }

            if (machine.Status is MachineStatus.OutOfService or MachineStatus.Error)
            {
                return Result<Order>.Failure("Machine is not available.");
            }

            if (machine.Maintenance.MaintenanceRequired)
            {
                return Result<Order>.Failure("Maintenance required.");
            }

            var product = machine.Products.FirstOrDefault(x => x.Id == request.ProductId && x.Enabled);
            if (product is null)
            {
                return Result<Order>.Failure("Product not found.");
            }

            var recipe = machine.Recipes.FirstOrDefault(x => x.Id == product.RecipeId);
            if (recipe is null)
            {
                return Result<Order>.Failure("Recipe not found.");
            }

            if (machine.Credit.CurrentCredit < product.Price)
            {
                return Result<Order>.Failure("Insufficient credit.");
            }

            if (!HasRequiredResources(machine, recipe))
            {
                return Result<Order>.Failure("Required resources are not available.");
            }

            if (machine.DispensingUnit.IsBusy)
            {
                return Result<Order>.Failure("Machine is busy.");
            }

            machine.Credit.CurrentCredit -= product.Price;
            machine.Credit.Transactions.Insert(0, new CreditTransaction
            {
                Amount = -product.Price,
                Description = $"Order payment: {product.Name}",
                TimestampUtc = clock.UtcNow,
                Type = "order-charge"
            });

            var order = new Order
            {
                Id = Guid.NewGuid().ToString("N"),
                ProductId = product.Id,
                ProductName = product.Name,
                RecipeId = recipe.Id,
                Status = OrderStatus.Validating,
                CreatedAtUtc = clock.UtcNow,
                UpdatedAtUtc = clock.UtcNow
            };
            machine.Orders.Insert(0, order);
            machine.Orders = machine.Orders.Take(100).ToList();
            machine.Metrics.TotalOrders++;
            machine.DispensingUnit.IsBusy = true;
            machine.DispensingUnit.CurrentOrderId = order.Id;
            machine.Boiler.TargetTemperature = recipe.TargetTemperature;
            machine.Status = MachineStatus.Heating;
            machine.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "order", Message = $"Order created: {order.Id} - {order.ProductName}" });
            return Result<Order>.Success(order.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("OrderCreated", result.Value!, cancellationToken);
            var credit = await repository.ReadAsync(machine => machine.Credit.CurrentCredit, cancellationToken);
            await eventBus.PublishAsync("CreditChanged", new CreditDto(credit), cancellationToken);
            await snapshotService.SaveSnapshotAsync("order-created", cancellationToken);
        }

        return result;
    }

    public Task<IReadOnlyCollection<Order>> GetOrdersAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<Order>)machine.Orders.Select(x => x.Clone()).ToList(), cancellationToken);

    public Task<Order?> GetOrderAsync(string id, CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.Orders.FirstOrDefault(x => x.Id == id)?.Clone(), cancellationToken);

    public async Task<Result<Order>> CancelOrderAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            var order = machine.Orders.FirstOrDefault(x => x.Id == id);
            if (order is null)
            {
                return Result<Order>.Failure("Order not found.");
            }

            if (order.Status is OrderStatus.Completed or OrderStatus.Failed or OrderStatus.Cancelled)
            {
                return Result<Order>.Failure("Order cannot be cancelled.");
            }

            order.Status = OrderStatus.Cancelled;
            order.UpdatedAtUtc = clock.UtcNow;
            order.CompletedAtUtc = clock.UtcNow;
            machine.Metrics.CancelledOrders++;
            machine.DispensingUnit.IsBusy = false;
            machine.DispensingUnit.CurrentOrderId = null;
            machine.Status = machine.PowerOn ? MachineStatus.Ready : MachineStatus.Off;
            return Result<Order>.Success(order.Clone());
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("OrderStatusChanged", result.Value!, cancellationToken);
            await snapshotService.SaveSnapshotAsync("order-cancelled", cancellationToken);
        }

        return result;
    }

    private static bool HasRequiredResources(CoffeeMachine.Domain.CoffeeMachine machine, Recipe recipe)
    {
        foreach (var step in recipe.Steps)
        {
            if (step.IngredientKey.Equals("water", StringComparison.OrdinalIgnoreCase))
            {
                if (machine.WaterTank.CurrentLevelMl < step.Quantity)
                {
                    return false;
                }

                continue;
            }

            var ingredient = machine.Ingredients.FirstOrDefault(x => x.Name.Equals(step.IngredientKey, StringComparison.OrdinalIgnoreCase));
            if (ingredient is null || !ingredient.Enabled || ingredient.CurrentLevel < step.Quantity)
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class ConfigApplicationService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IStateSnapshotService snapshotService) : IConfigApplicationService
{
    public Task<ConfigDto> GetConfigAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => new ConfigDto(machine.Id, machine.Maintenance.MaintenanceThreshold), cancellationToken);

    public async Task<Result<ConfigDto>> UpdateConfigAsync(ConfigDto request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            machine.Id = request.MachineId;
            machine.Maintenance.MaintenanceThreshold = request.MaintenanceThreshold;
            return Result<ConfigDto>.Success(request);
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ConfigurationChanged", request, cancellationToken);
            await snapshotService.SaveSnapshotAsync("config-update", cancellationToken);
        }

        return result;
    }

    public Task<SimulationConfigDto> GetSimulationConfigAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => new SimulationConfigDto(
            machine.SimulationConfig.TickIntervalMs,
            machine.SimulationConfig.HeatingRatePerTick,
            machine.SimulationConfig.CoolingRatePerTick,
            machine.SimulationConfig.HeatingTimeoutSeconds,
            machine.SimulationConfig.ProcessFailureProbability,
            machine.SimulationConfig.MaximumBoilerTemperature,
            machine.SimulationConfig.AutoSaveEnabled,
            machine.SimulationConfig.AutoSaveIntervalSeconds), cancellationToken);

    public async Task<Result<SimulationConfigDto>> UpdateSimulationConfigAsync(SimulationConfigDto request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            machine.SimulationConfig.TickIntervalMs = request.TickIntervalMs;
            machine.SimulationConfig.HeatingRatePerTick = request.HeatingRatePerTick;
            machine.SimulationConfig.CoolingRatePerTick = request.CoolingRatePerTick;
            machine.SimulationConfig.HeatingTimeoutSeconds = request.HeatingTimeoutSeconds;
            machine.SimulationConfig.ProcessFailureProbability = request.ProcessFailureProbability;
            machine.SimulationConfig.MaximumBoilerTemperature = request.MaximumBoilerTemperature;
            machine.SimulationConfig.AutoSaveEnabled = request.AutoSaveEnabled;
            machine.SimulationConfig.AutoSaveIntervalSeconds = request.AutoSaveIntervalSeconds;
            return Result<SimulationConfigDto>.Success(request);
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("ConfigurationChanged", request, cancellationToken);
            await snapshotService.SaveSnapshotAsync("simulation-config-update", cancellationToken);
        }

        return result;
    }
}

public sealed class DiagnosticsApplicationService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IStateSnapshotService snapshotService,
    ILogReader logReader,
    IClock clock) : IDiagnosticsApplicationService
{
    public Task<IReadOnlyCollection<DiagnosticRecord>> GetActiveErrorsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<DiagnosticRecord>)machine.Diagnostics
            .Where(x => x.Active && x.Severity == DiagnosticSeverity.Error)
            .Select(x => x.Clone())
            .ToList(), cancellationToken);

    public Task<IReadOnlyCollection<DiagnosticRecord>> GetActiveWarningsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<DiagnosticRecord>)machine.Diagnostics
            .Where(x => x.Active && x.Severity == DiagnosticSeverity.Warning)
            .Select(x => x.Clone())
            .ToList(), cancellationToken);

    public Task<MaintenanceInfo> GetMaintenanceStatusAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.Maintenance.Clone(), cancellationToken);

    public Task<MetricsState> GetMetricsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => machine.Metrics.Clone(), cancellationToken);

    public Task<IReadOnlyCollection<string>> GetRecentLogsAsync(int lines, CancellationToken cancellationToken = default) =>
        logReader.ReadRecentAsync(lines, cancellationToken);

    public async Task<Result> InjectFaultAsync(FaultInjectionRequest request, CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            ApplyFault(machine, request.FaultType, request.Message ?? request.FaultType.ToString(), clock.UtcNow);
            return Result.Success();
        }, cancellationToken);

        await eventBus.PublishAsync("ErrorRaised", request, cancellationToken);
        await snapshotService.SaveSnapshotAsync("fault-injected", cancellationToken);
        return result;
    }

    private static void ApplyFault(CoffeeMachine.Domain.CoffeeMachine machine, FaultType faultType, string message, DateTimeOffset now)
    {
        switch (faultType)
        {
            case FaultType.PowerFailure:
                machine.PowerFaultActive = true;
                machine.PowerOn = false;
                machine.Status = MachineStatus.Error;
                break;
            case FaultType.HeatingFailure:
                machine.Boiler.HeatingFaultActive = true;
                machine.Status = MachineStatus.Error;
                break;
            case FaultType.Overheat:
                machine.Boiler.OverheatProtectionTriggered = true;
                machine.Status = MachineStatus.Error;
                break;
            case FaultType.WaterEmpty:
                machine.WaterTank.CurrentLevelMl = 0;
                break;
            case FaultType.IngredientDepleted:
                if (machine.Ingredients.Count > 0)
                {
                    machine.Ingredients[0].CurrentLevel = 0;
                }
                break;
            case FaultType.DispensingFailure:
                machine.DispensingUnit.FaultActive = true;
                machine.Status = MachineStatus.Error;
                break;
            case FaultType.MaintenanceLock:
                machine.Maintenance.MaintenanceRequired = true;
                machine.Status = MachineStatus.MaintenanceRequired;
                break;
        }

        machine.Diagnostics.Insert(0, new DiagnosticRecord
        {
            Code = faultType.ToString(),
            Message = message,
            Severity = DiagnosticSeverity.Error,
            Active = true,
            CreatedAtUtc = now
        });
    }
}

public sealed class StateApplicationService(
    IStatePersistenceService persistenceService,
    IStateRestoreService restoreService) : IStateApplicationService
{
    public async Task<Result> SaveAsync(CancellationToken cancellationToken = default)
    {
        await persistenceService.SaveAsync(cancellationToken);
        return Result.Success();
    }

    public async Task<Result<StateRestoreResult>> ReloadAsync(CancellationToken cancellationToken = default) =>
        Result<StateRestoreResult>.Success(await restoreService.RestoreAsync(cancellationToken));

    public Task<string> ExportAsync(CancellationToken cancellationToken = default) =>
        persistenceService.ExportAsync(cancellationToken);
}
