using System.Text;
using System.Text.Json;
using CoffeeMachine.Application;
using CoffeeMachine.Domain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MachineState = CoffeeMachine.Domain.CoffeeMachine;

namespace CoffeeMachine.Infrastructure.Persistence;

public sealed class PersistenceOptions
{
    public string SnapshotPath { get; set; } = "data/machine-state.json";
}

public sealed class FileStorage : IFileStorage
{
    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(File.Exists(path));

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
        File.ReadAllTextAsync(path, cancellationToken);

    public async Task WriteAllTextAtomicAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var tempPath = $"{path}.tmp";
        await File.WriteAllTextAsync(tempPath, content, Encoding.UTF8, cancellationToken);

        if (File.Exists(path))
        {
            var backup = $"{path}.bak";
            File.Replace(tempPath, path, backup, true);
        }
        else
        {
            File.Move(tempPath, path, true);
        }
    }
}

public sealed class JsonStateSnapshotSerializer : IStateSnapshotSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string Serialize(MachineState machine) => JsonSerializer.Serialize(machine, Options);

    public MachineState Deserialize(string json) =>
        JsonSerializer.Deserialize<MachineState>(json, Options)
        ?? throw new InvalidOperationException("Snapshot deserialization returned null.");
}

public sealed class JsonStatePersistenceService(
    ICoffeeMachineRuntimeRepository repository,
    IFileStorage fileStorage,
    IStateSnapshotSerializer serializer,
    IClock clock,
    IEventBus eventBus,
    ILogger<JsonStatePersistenceService> logger,
    IOptions<PersistenceOptions> options) : IStatePersistenceService, IStateRestoreService, IStateSnapshotService
{
    private readonly string _snapshotPath = options.Value.SnapshotPath;
    private const string CurrentSnapshotVersion = "1.0";

    public async Task SaveAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.ReadAsync(machine =>
        {
            var clone = machine.Clone();
            clone.Metrics.SnapshotSaveCount++;
            return clone;
        }, cancellationToken);

        snapshot.SnapshotVersion = CurrentSnapshotVersion;
        var json = serializer.Serialize(snapshot);
        await fileStorage.WriteAllTextAtomicAsync(_snapshotPath, json, cancellationToken);
        logger.LogInformation("Snapshot saved to {Path}", _snapshotPath);
        await eventBus.PublishAsync("SnapshotSaved", new { path = _snapshotPath, atUtc = clock.UtcNow }, cancellationToken);
    }

    public Task SaveSnapshotAsync(string reason, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Snapshot requested. Reason: {Reason}", reason);
        return SaveAsync(cancellationToken);
    }

    public async Task<string> ExportAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = await repository.ReadAsync(machine => machine.Clone(), cancellationToken);
        return serializer.Serialize(snapshot);
    }

    public async Task<StateRestoreResult> RestoreAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!await fileStorage.ExistsAsync(_snapshotPath, cancellationToken))
            {
                var seed = MachineSeedFactory.Create(clock.UtcNow);
                seed.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "bootstrap", Message = "Seed state created because snapshot file was not found." });
                await repository.ReplaceAsync(seed, cancellationToken);
                logger.LogInformation("Snapshot file not found. Seeded initial state.");
                return new StateRestoreResult(false, true, "Snapshot not found. Seed applied.");
            }

            var content = await fileStorage.ReadAllTextAsync(_snapshotPath, cancellationToken);
            var restored = serializer.Deserialize(content);

            if (restored.SnapshotVersion != CurrentSnapshotVersion)
            {
                throw new InvalidOperationException($"Unsupported snapshot version '{restored.SnapshotVersion}'.");
            }

            if (string.IsNullOrWhiteSpace(restored.Id) || restored.Boiler is null || restored.WaterTank is null)
            {
                throw new InvalidOperationException("Snapshot validation failed.");
            }

            restored.Metrics.SnapshotRestoreCount++;
            restored.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "bootstrap", Message = "Snapshot restored successfully." });
            await repository.ReplaceAsync(restored, cancellationToken);
            logger.LogInformation("Snapshot restored from {Path}", _snapshotPath);
            await eventBus.PublishAsync("SnapshotRestored", new { path = _snapshotPath, atUtc = clock.UtcNow }, cancellationToken);
            return new StateRestoreResult(true, false, "Snapshot restored.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Snapshot restore failed. Falling back to seed.");
            var seed = MachineSeedFactory.Create(clock.UtcNow);
            seed.RecentEvents.Insert(0, new EventLogEntry { TimestampUtc = clock.UtcNow, Category = "bootstrap", Message = "Seed state created after invalid snapshot." });
            await repository.ReplaceAsync(seed, cancellationToken);
            return new StateRestoreResult(false, true, "Invalid snapshot. Seed applied.");
        }
    }
}
