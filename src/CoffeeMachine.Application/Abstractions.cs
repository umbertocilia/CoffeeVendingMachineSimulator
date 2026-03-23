using CoffeeMachine.Domain;

namespace CoffeeMachine.Application;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public interface IRandomProvider
{
    double NextDouble();
}

public interface IEventBus
{
    Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default);
}

public interface IRealtimeNotifier
{
    Task NotifyAsync(string eventName, object payload, CancellationToken cancellationToken = default);
}

public interface ICoffeeMachineRuntimeRepository
{
    Task<T> ReadAsync<T>(Func<CoffeeMachine.Domain.CoffeeMachine, T> reader, CancellationToken cancellationToken = default);
    Task<T> WriteAsync<T>(Func<CoffeeMachine.Domain.CoffeeMachine, T> writer, CancellationToken cancellationToken = default);
    Task ReplaceAsync(CoffeeMachine.Domain.CoffeeMachine machine, CancellationToken cancellationToken = default);
}

public interface IStateSnapshotSerializer
{
    string Serialize(CoffeeMachine.Domain.CoffeeMachine machine);
    CoffeeMachine.Domain.CoffeeMachine Deserialize(string json);
}

public interface IFileStorage
{
    Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default);
    Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
    Task WriteAllTextAtomicAsync(string path, string content, CancellationToken cancellationToken = default);
}

public interface IStatePersistenceService
{
    Task SaveAsync(CancellationToken cancellationToken = default);
    Task<string> ExportAsync(CancellationToken cancellationToken = default);
}

public interface IStateRestoreService
{
    Task<StateRestoreResult> RestoreAsync(CancellationToken cancellationToken = default);
}

public interface IStateSnapshotService
{
    Task SaveSnapshotAsync(string reason, CancellationToken cancellationToken = default);
}

public interface ILogReader
{
    Task<IReadOnlyCollection<string>> ReadRecentAsync(int lines, CancellationToken cancellationToken = default);
}

public record StateRestoreResult(bool RestoredFromSnapshot, bool FallbackToSeed, string Message);
