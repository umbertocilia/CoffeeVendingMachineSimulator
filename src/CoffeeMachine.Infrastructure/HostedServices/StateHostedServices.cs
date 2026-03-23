using CoffeeMachine.Application;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CoffeeMachine.Infrastructure.HostedServices;

public sealed class StateBootstrapHostedService(
    IStateRestoreService restoreService,
    ILogger<StateBootstrapHostedService> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var result = await restoreService.RestoreAsync(cancellationToken);
        logger.LogInformation("State bootstrap completed. {Message}", result.Message);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PeriodicSnapshotHostedService(
    ICoffeeMachineRuntimeRepository repository,
    IStateSnapshotService snapshotService,
    ILogger<PeriodicSnapshotHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var config = await repository.ReadAsync(machine => machine.SimulationConfig.Clone(), stoppingToken);
            var delay = Math.Max(5, config.AutoSaveIntervalSeconds);
            await Task.Delay(TimeSpan.FromSeconds(delay), stoppingToken);

            if (!config.AutoSaveEnabled)
            {
                continue;
            }

            logger.LogInformation("Periodic snapshot save triggered.");
            await snapshotService.SaveSnapshotAsync("periodic-autosave", stoppingToken);
        }
    }
}
