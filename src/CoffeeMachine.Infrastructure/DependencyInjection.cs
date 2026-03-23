using CoffeeMachine.Application;
using CoffeeMachine.Domain;
using CoffeeMachine.Infrastructure.Diagnostics;
using CoffeeMachine.Infrastructure.HostedServices;
using CoffeeMachine.Infrastructure.Persistence;
using CoffeeMachine.Infrastructure.Realtime;
using CoffeeMachine.Infrastructure.Runtime;
using CoffeeMachine.Infrastructure.Simulation;
using CoffeeMachine.Infrastructure.System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeMachine.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddCoffeeMachineInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<PersistenceOptions>(configuration.GetSection("Persistence"));
        services.Configure<LogFileOptions>(configuration.GetSection("Logging:File"));

        services.AddSingleton(MachineSeedFactory.Create(DateTimeOffset.UtcNow));
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IRandomProvider, RandomProvider>();
        services.AddSingleton<ICoffeeMachineRuntimeRepository, InMemoryCoffeeMachineRuntimeRepository>();
        services.AddSingleton<IFileStorage, FileStorage>();
        services.AddSingleton<IStateSnapshotSerializer, JsonStateSnapshotSerializer>();
        services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddSingleton<ILogReader, FileLogReader>();
        services.AddSingleton<IStatePersistenceService, JsonStatePersistenceService>();
        services.AddSingleton<IStateRestoreService, JsonStatePersistenceService>();
        services.AddSingleton<IStateSnapshotService, JsonStatePersistenceService>();

        services.AddHostedService<StateBootstrapHostedService>();
        services.AddHostedService<SimulationBackgroundService>();
        services.AddHostedService<PeriodicSnapshotHostedService>();
        return services;
    }

    public static IServiceCollection AddCoffeeMachineApplication(this IServiceCollection services)
    {
        services.AddScoped<IMachineApplicationService, MachineApplicationService>();
        services.AddScoped<ICreditApplicationService, CreditApplicationService>();
        services.AddScoped<ICatalogApplicationService, CatalogApplicationService>();
        services.AddScoped<IOrderApplicationService, OrderApplicationService>();
        services.AddScoped<IConfigApplicationService, ConfigApplicationService>();
        services.AddScoped<IDiagnosticsApplicationService, DiagnosticsApplicationService>();
        services.AddScoped<IStateApplicationService, StateApplicationService>();
        return services;
    }
}
