using CoffeeMachine.Application;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace CoffeeMachine.Infrastructure.Realtime;

public sealed class SignalRRealtimeNotifier(
    IHubContext<MachineHub> hubContext,
    ILogger<SignalRRealtimeNotifier> logger) : IRealtimeNotifier
{
    public async Task NotifyAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Realtime event emitted: {EventName}", eventName);
        await hubContext.Clients.All.SendAsync(eventName, payload, cancellationToken);
    }
}

public sealed class EventBus(
    IRealtimeNotifier realtimeNotifier,
    ILogger<EventBus> logger) : IEventBus
{
    public async Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Event published: {EventName}", eventName);
        await realtimeNotifier.NotifyAsync(eventName, payload, cancellationToken);
    }
}
