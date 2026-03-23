using CoffeeMachine.Application;
using CoffeeMachine.Domain;
using Microsoft.Extensions.Hosting;

namespace CoffeeMachine.Infrastructure.Simulation;

public sealed class SimulationBackgroundService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IRandomProvider randomProvider,
    IClock clock) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var tickInterval = await repository.ReadAsync(machine => machine.SimulationConfig.TickIntervalMs, stoppingToken);
            await RunSingleTickAsync(stoppingToken);
            await Task.Delay(Math.Max(100, tickInterval), stoppingToken);
        }
    }

    // Exposed to keep the simulation loop testable without relying on time-based hosted execution.
    public async Task RunSingleTickAsync(CancellationToken cancellationToken = default)
    {
        var tickEvents = await repository.WriteAsync(machine =>
        {
            var emitted = new List<(string Name, object Payload)>();
            var now = clock.UtcNow;
            machine.Metrics.LastSimulationTickUtc = now;

            UpdateTemperature(machine, emitted, now);
            UpdateOrderProgress(machine, emitted, now);
            UpdateSensors(machine);
            UpdateWarnings(machine, emitted, now);
            UpdateMachineStatus(machine);

            return emitted;
        }, cancellationToken);

        foreach (var emitted in tickEvents)
        {
            await eventBus.PublishAsync(emitted.Name, emitted.Payload, cancellationToken);
        }
    }

    private void UpdateTemperature(CoffeeMachine.Domain.CoffeeMachine machine, List<(string Name, object Payload)> emitted, DateTimeOffset now)
    {
        if (!machine.PowerOn)
        {
            machine.Boiler.CurrentTemperature = Math.Max(22, machine.Boiler.CurrentTemperature - machine.SimulationConfig.CoolingRatePerTick);
        }
        else if (machine.Boiler.HeatingFaultActive)
        {
            machine.Boiler.CurrentTemperature = Math.Max(22, machine.Boiler.CurrentTemperature - 0.25);
        }
        else if (machine.Boiler.CurrentTemperature < machine.Boiler.TargetTemperature)
        {
            machine.Boiler.HeatingStartedAtUtc ??= now;
            machine.Boiler.CurrentTemperature = Math.Min(machine.Boiler.TargetTemperature, machine.Boiler.CurrentTemperature + machine.SimulationConfig.HeatingRatePerTick);
        }
        else
        {
            machine.Boiler.CurrentTemperature = Math.Max(machine.Boiler.TargetTemperature, machine.Boiler.CurrentTemperature - 0.2);
        }

        if (machine.Boiler.CurrentTemperature >= machine.SimulationConfig.MaximumBoilerTemperature)
        {
            machine.Boiler.OverheatProtectionTriggered = true;
            AddOrActivateDiagnostic(machine, "Overheat", "Boiler overheat protection triggered.", DiagnosticSeverity.Error, now);
        }

        if (machine.Boiler.HeatingStartedAtUtc.HasValue &&
            machine.PowerOn &&
            machine.Boiler.CurrentTemperature < machine.Boiler.TargetTemperature &&
            now - machine.Boiler.HeatingStartedAtUtc.Value > TimeSpan.FromSeconds(machine.SimulationConfig.HeatingTimeoutSeconds))
        {
            machine.Boiler.HeatingFaultActive = true;
            AddOrActivateDiagnostic(machine, "HeatingTimeout", "Boiler target temperature timeout.", DiagnosticSeverity.Error, now);
        }

        emitted.Add(("TemperatureChanged", new
        {
            currentTemperature = machine.Boiler.CurrentTemperature,
            targetTemperature = machine.Boiler.TargetTemperature,
            status = machine.Status.ToString()
        }));
    }

    private void UpdateOrderProgress(CoffeeMachine.Domain.CoffeeMachine machine, List<(string Name, object Payload)> emitted, DateTimeOffset now)
    {
        var activeOrder = machine.Orders.FirstOrDefault(order =>
            order.Status is OrderStatus.Validating or OrderStatus.WaitingForHeat or OrderStatus.DispensingIngredient or OrderStatus.Mixing);

        if (activeOrder is null)
        {
            machine.DispensingUnit.IsBusy = false;
            machine.DispensingUnit.CurrentOrderId = null;
            return;
        }

        var recipe = machine.Recipes.FirstOrDefault(x => x.Id == activeOrder.RecipeId);
        if (recipe is null)
        {
            activeOrder.Status = OrderStatus.Failed;
            activeOrder.FailureReason = "Recipe missing.";
            activeOrder.CompletedAtUtc = now;
            machine.Metrics.FailedOrders++;
            machine.DispensingUnit.IsBusy = false;
            machine.DispensingUnit.CurrentOrderId = null;
            emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            return;
        }

        if (machine.DispensingUnit.FaultActive || machine.PowerFaultActive || machine.Boiler.OverheatProtectionTriggered)
        {
            FailOrder(machine, activeOrder, "Machine fault during dispensing.", now);
            emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            return;
        }

        if (activeOrder.Status == OrderStatus.Validating)
        {
            activeOrder.Status = OrderStatus.WaitingForHeat;
            activeOrder.UpdatedAtUtc = now;
            emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            return;
        }

        if (activeOrder.Status == OrderStatus.WaitingForHeat)
        {
            if (machine.Boiler.CurrentTemperature >= recipe.TargetTemperature - 0.5)
            {
                activeOrder.Status = OrderStatus.DispensingIngredient;
                activeOrder.CurrentStepIndex = 0;
                activeOrder.CurrentStepElapsedMs = 0;
                activeOrder.UpdatedAtUtc = now;
                emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            }

            return;
        }

        if (activeOrder.CurrentStepIndex >= recipe.Steps.Count)
        {
            var wasMixing = activeOrder.Status == OrderStatus.Mixing;
            activeOrder.Status = OrderStatus.Mixing;
            activeOrder.CurrentStepElapsedMs += machine.SimulationConfig.TickIntervalMs;
            activeOrder.ProgressPercentage = 95;

            if (!wasMixing)
            {
                activeOrder.UpdatedAtUtc = now;
                emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            }

            if (activeOrder.CurrentStepElapsedMs >= 800)
            {
                activeOrder.Status = OrderStatus.Completed;
                activeOrder.ProgressPercentage = 100;
                activeOrder.UpdatedAtUtc = now;
                activeOrder.CompletedAtUtc = now;
                machine.Metrics.CompletedOrders++;
                machine.Maintenance.DispenseCount++;
                machine.Maintenance.WearPercentage = Math.Min(100, machine.Maintenance.WearPercentage + 2.5);
                machine.Maintenance.MaintenanceRequired = MaintenancePolicy.IsMaintenanceRequired(machine.Maintenance.DispenseCount, machine.Maintenance.MaintenanceThreshold);
                machine.DispensingUnit.IsBusy = false;
                machine.DispensingUnit.CurrentOrderId = null;
                emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            }

            return;
        }

        activeOrder.Status = OrderStatus.DispensingIngredient;
        var step = recipe.Steps[activeOrder.CurrentStepIndex];
        activeOrder.CurrentStepElapsedMs += machine.SimulationConfig.TickIntervalMs;
        activeOrder.ProgressPercentage = Math.Round(((activeOrder.CurrentStepIndex + (double)activeOrder.CurrentStepElapsedMs / step.DurationMs) / (recipe.Steps.Count + 1)) * 100, 2);

        if (randomProvider.NextDouble() < machine.SimulationConfig.ProcessFailureProbability)
        {
            FailOrder(machine, activeOrder, "Random dispensing failure.", now);
            machine.DispensingUnit.FaultActive = true;
            AddOrActivateDiagnostic(machine, "DispensingFailure", "Random dispensing failure.", DiagnosticSeverity.Error, now);
            emitted.Add(("ErrorRaised", new { code = "DispensingFailure", orderId = activeOrder.Id }));
            emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            return;
        }

        if (activeOrder.CurrentStepElapsedMs < step.DurationMs)
        {
            emitted.Add(("DispensingProgressChanged", new { activeOrder.Id, activeOrder.ProgressPercentage, activeOrder.Status }));
            return;
        }

        if (!ConsumeStep(machine, step, now))
        {
            FailOrder(machine, activeOrder, $"Step resource unavailable: {step.IngredientKey}", now);
            emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
            return;
        }

        activeOrder.CurrentStepIndex++;
        activeOrder.CurrentStepElapsedMs = 0;
        activeOrder.UpdatedAtUtc = now;

        if (activeOrder.CurrentStepIndex >= recipe.Steps.Count)
        {
            activeOrder.Status = OrderStatus.Mixing;
            emitted.Add(("OrderStatusChanged", activeOrder.Clone()));
        }

        emitted.Add(("IngredientLevelChanged", new { ingredient = step.IngredientKey }));
        emitted.Add(("DispensingProgressChanged", new { activeOrder.Id, activeOrder.ProgressPercentage, activeOrder.Status }));
    }

    private static bool ConsumeStep(CoffeeMachine.Domain.CoffeeMachine machine, RecipeStep step, DateTimeOffset now)
    {
        if (step.IngredientKey.Equals("water", StringComparison.OrdinalIgnoreCase))
        {
            if (machine.WaterTank.CurrentLevelMl < step.Quantity)
            {
                AddOrActivateDiagnostic(machine, "WaterEmpty", "Water tank empty.", DiagnosticSeverity.Error, now);
                return false;
            }

            machine.WaterTank.CurrentLevelMl -= step.Quantity;
            return true;
        }

        var ingredient = machine.Ingredients.FirstOrDefault(x => x.Name.Equals(step.IngredientKey, StringComparison.OrdinalIgnoreCase));
        if (ingredient is null || ingredient.CurrentLevel < step.Quantity)
        {
            AddOrActivateDiagnostic(machine, "IngredientDepleted", $"Ingredient depleted: {step.IngredientKey}.", DiagnosticSeverity.Error, now);
            return false;
        }

        ingredient.CurrentLevel -= step.Quantity;
        return true;
    }

    private static void FailOrder(CoffeeMachine.Domain.CoffeeMachine machine, Order order, string reason, DateTimeOffset now)
    {
        order.Status = OrderStatus.Failed;
        order.FailureReason = reason;
        order.UpdatedAtUtc = now;
        order.CompletedAtUtc = now;
        machine.Metrics.FailedOrders++;
        machine.DispensingUnit.IsBusy = false;
        machine.DispensingUnit.CurrentOrderId = null;
    }

    private static void UpdateWarnings(CoffeeMachine.Domain.CoffeeMachine machine, List<(string Name, object Payload)> emitted, DateTimeOffset now)
    {
        if (machine.WaterTank.CurrentLevelMl <= machine.WaterTank.LowLevelThresholdMl)
        {
            AddOrActivateDiagnostic(machine, "WaterLow", "Water tank level is low.", DiagnosticSeverity.Warning, now);
        }
        else
        {
            ResolveDiagnostic(machine, "WaterLow", now, emitted);
        }

        foreach (var ingredient in machine.Ingredients)
        {
            if (ingredient.CurrentLevel <= ingredient.LowLevelThreshold)
            {
                AddOrActivateDiagnostic(machine, $"Low-{ingredient.Id}", $"Ingredient low: {ingredient.Name}.", DiagnosticSeverity.Warning, now);
            }
            else
            {
                ResolveDiagnostic(machine, $"Low-{ingredient.Id}", now, emitted);
            }
        }
    }

    private static void UpdateSensors(CoffeeMachine.Domain.CoffeeMachine machine)
    {
        machine.Sensors.BoilerTemperature = machine.Boiler.CurrentTemperature;
        machine.Sensors.WaterLevelPercentage = machine.WaterTank.CapacityMl == 0 ? 0 : Math.Round((machine.WaterTank.CurrentLevelMl / machine.WaterTank.CapacityMl) * 100, 2);
        machine.Sensors.AverageIngredientPercentage = machine.Ingredients.Count == 0
            ? 0
            : Math.Round(machine.Ingredients.Average(x => x.Capacity == 0 ? 0 : (x.CurrentLevel / x.Capacity) * 100), 2);
    }

    private static void UpdateMachineStatus(CoffeeMachine.Domain.CoffeeMachine machine)
    {
        if (!machine.PowerOn)
        {
            machine.Status = MachineStatus.Off;
            return;
        }

        if (machine.Diagnostics.Any(x => x.Active && x.Severity == DiagnosticSeverity.Error))
        {
            machine.Status = MachineStatus.Error;
            return;
        }

        if (machine.Maintenance.MaintenanceRequired)
        {
            machine.Status = MachineStatus.MaintenanceRequired;
            return;
        }

        if (machine.DispensingUnit.IsBusy)
        {
            machine.Status = MachineStatus.Dispensing;
            return;
        }

        machine.Status = machine.Boiler.CurrentTemperature < machine.Boiler.TargetTemperature - 0.5
            ? MachineStatus.Heating
            : MachineStatus.Ready;
    }

    private static void AddOrActivateDiagnostic(CoffeeMachine.Domain.CoffeeMachine machine, string code, string message, DiagnosticSeverity severity, DateTimeOffset now)
    {
        var existing = machine.Diagnostics.FirstOrDefault(x => x.Code == code);
        if (existing is null)
        {
            machine.Diagnostics.Insert(0, new DiagnosticRecord
            {
                Code = code,
                Message = message,
                Severity = severity,
                Active = true,
                CreatedAtUtc = now
            });
            machine.Diagnostics = machine.Diagnostics.Take(100).ToList();
            return;
        }

        existing.Message = message;
        existing.Severity = severity;
        existing.Active = true;
        existing.ResolvedAtUtc = null;
    }

    private static void ResolveDiagnostic(CoffeeMachine.Domain.CoffeeMachine machine, string code, DateTimeOffset now, List<(string Name, object Payload)> emitted)
    {
        var existing = machine.Diagnostics.FirstOrDefault(x => x.Code == code && x.Active);
        if (existing is null)
        {
            return;
        }

        existing.Active = false;
        existing.ResolvedAtUtc = now;
        emitted.Add(("ErrorResolved", new { code }));
    }
}
