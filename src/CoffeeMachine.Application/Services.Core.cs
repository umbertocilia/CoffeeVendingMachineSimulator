using CoffeeMachine.Application.Results;
using CoffeeMachine.Domain;

namespace CoffeeMachine.Application;

public sealed class MachineApplicationService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IStateSnapshotService snapshotService,
    IClock clock) : IMachineApplicationService
{
    public Task<MachineStatusDto> GetStatusAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => new MachineStatusDto(
            machine.Id,
            machine.Status,
            machine.PowerOn,
            machine.Boiler.CurrentTemperature,
            machine.Boiler.TargetTemperature,
            machine.WaterTank.CurrentLevelMl,
            machine.Credit.CurrentCredit,
            machine.Maintenance.MaintenanceRequired,
            machine.Diagnostics.Where(x => x.Active && x.Severity == DiagnosticSeverity.Error).Select(x => x.Code).ToList(),
            machine.Diagnostics.Where(x => x.Active && x.Severity == DiagnosticSeverity.Warning).Select(x => x.Code).ToList()), cancellationToken);

    public Task<MachineComponentsDto> GetComponentsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => new MachineComponentsDto(
            machine.Boiler.Clone(),
            machine.WaterTank.Clone(),
            machine.Ingredients.Select(x => x.Clone()).ToList(),
            machine.DispensingUnit.Clone(),
            machine.Sensors.Clone()), cancellationToken);

    public Task<DiagnosticsDto> GetDiagnosticsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => new DiagnosticsDto(
            machine.Diagnostics.Where(x => x.Active && x.Severity == DiagnosticSeverity.Error).Select(x => x.Clone()).ToList(),
            machine.Diagnostics.Where(x => x.Active && x.Severity == DiagnosticSeverity.Warning).Select(x => x.Clone()).ToList(),
            machine.RecentEvents.OrderByDescending(x => x.TimestampUtc).Take(100).Select(x => x.Clone()).ToList(),
            machine.Metrics.Clone(),
            machine.Maintenance.Clone()), cancellationToken);

    public async Task<Result> PowerOnAsync(CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            if (machine.PowerFaultActive)
            {
                return Result.Failure("Power fault active.");
            }

            machine.PowerOn = true;
            machine.Status = MachineStatus.Initializing;
            machine.Boiler.HeatingStartedAtUtc = clock.UtcNow;
            AddEvent(machine, "machine", "Machine powered on.");
            return Result.Success();
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("MachineStateChanged", new { status = MachineStatus.Initializing }, cancellationToken);
            await snapshotService.SaveSnapshotAsync("power-on", cancellationToken);
        }

        return result;
    }

    public async Task<Result> PowerOffAsync(CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            machine.PowerOn = false;
            machine.Status = MachineStatus.Off;
            machine.DispensingUnit.IsBusy = false;
            machine.DispensingUnit.CurrentOrderId = null;
            AddEvent(machine, "machine", "Machine powered off.");
            return Result.Success();
        }, cancellationToken);

        await eventBus.PublishAsync("MachineStateChanged", new { status = MachineStatus.Off }, cancellationToken);
        await snapshotService.SaveSnapshotAsync("power-off", cancellationToken);
        return result;
    }

    public async Task<Result> ResetAsync(CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            machine.PowerFaultActive = false;
            machine.Boiler.HeatingFaultActive = false;
            machine.Boiler.OverheatProtectionTriggered = false;
            machine.DispensingUnit.FaultActive = false;
            foreach (var diagnostic in machine.Diagnostics.Where(x => x.Active))
            {
                diagnostic.Active = false;
                diagnostic.ResolvedAtUtc = clock.UtcNow;
            }

            machine.Status = machine.PowerOn ? MachineStatus.Initializing : MachineStatus.Off;
            AddEvent(machine, "machine", "Machine reset executed.");
            return Result.Success();
        }, cancellationToken);

        await eventBus.PublishAsync("ErrorResolved", new { code = "ALL" }, cancellationToken);
        await snapshotService.SaveSnapshotAsync("machine-reset", cancellationToken);
        return result;
    }

    public async Task<Result> ResetMaintenanceAsync(CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            machine.Maintenance.MaintenanceRequired = false;
            machine.Maintenance.WearPercentage = 0;
            machine.Maintenance.DispenseCount = 0;
            machine.Maintenance.LastMaintenanceAtUtc = clock.UtcNow;
            machine.Status = machine.PowerOn ? MachineStatus.Ready : MachineStatus.Off;
            AddEvent(machine, "maintenance", "Maintenance reset executed.");
            return Result.Success();
        }, cancellationToken);

        await eventBus.PublishAsync("MaintenanceStatusChanged", new { required = false }, cancellationToken);
        await snapshotService.SaveSnapshotAsync("maintenance-reset", cancellationToken);
        return result;
    }

    private void AddEvent(CoffeeMachine.Domain.CoffeeMachine machine, string category, string message)
    {
        machine.RecentEvents.Insert(0, new EventLogEntry
        {
            TimestampUtc = clock.UtcNow,
            Category = category,
            Message = message
        });
        machine.RecentEvents = machine.RecentEvents.Take(200).ToList();
    }
}

public sealed class CreditApplicationService(
    ICoffeeMachineRuntimeRepository repository,
    IEventBus eventBus,
    IStateSnapshotService snapshotService,
    IClock clock) : ICreditApplicationService
{
    public Task<CreditDto> GetCreditAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => new CreditDto(machine.Credit.CurrentCredit), cancellationToken);

    public Task<IReadOnlyCollection<CreditTransaction>> GetTransactionsAsync(CancellationToken cancellationToken = default) =>
        repository.ReadAsync(machine => (IReadOnlyCollection<CreditTransaction>)machine.Credit.Transactions
            .OrderByDescending(x => x.TimestampUtc)
            .Select(x => x.Clone())
            .ToList(), cancellationToken);

    public async Task<Result<CreditDto>> AddCreditAsync(AddCreditRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            return Result<CreditDto>.Failure("Amount must be greater than zero.");
        }

        var result = await repository.WriteAsync(machine =>
        {
            machine.Credit.CurrentCredit += request.Amount;
            machine.Metrics.TotalCreditInserted += request.Amount;
            machine.Credit.Transactions.Insert(0, new CreditTransaction
            {
                Amount = request.Amount,
                Description = request.Description,
                TimestampUtc = clock.UtcNow,
                Type = "credit-add"
            });
            machine.Credit.Transactions = machine.Credit.Transactions.Take(100).ToList();
            machine.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "credit", Message = $"Credit added: {request.Amount:0.00}" });
            return Result<CreditDto>.Success(new CreditDto(machine.Credit.CurrentCredit));
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await eventBus.PublishAsync("CreditChanged", result.Value!, cancellationToken);
            await snapshotService.SaveSnapshotAsync("credit-add", cancellationToken);
        }

        return result;
    }

    public async Task<Result<CreditDto>> ResetCreditAsync(CancellationToken cancellationToken = default)
    {
        var result = await repository.WriteAsync(machine =>
        {
            machine.Credit.CurrentCredit = 0;
            machine.Credit.Transactions.Insert(0, new CreditTransaction
            {
                Amount = 0,
                Description = "Credit reset",
                TimestampUtc = clock.UtcNow,
                Type = "credit-reset"
            });
            return Result<CreditDto>.Success(new CreditDto(0));
        }, cancellationToken);

        await eventBus.PublishAsync("CreditChanged", result.Value!, cancellationToken);
        await snapshotService.SaveSnapshotAsync("credit-reset", cancellationToken);
        return result;
    }
}
