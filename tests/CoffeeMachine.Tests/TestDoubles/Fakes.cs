using System.Collections.Concurrent;
using CoffeeMachine.Application;
using CoffeeMachine.Domain;

namespace CoffeeMachine.Tests.TestDoubles;

public sealed class FakeClock(DateTimeOffset? now = null) : IClock
{
    public DateTimeOffset UtcNow { get; private set; } = now ?? new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero);

    public void Advance(TimeSpan delta) => UtcNow = UtcNow.Add(delta);
}

public sealed class FakeRandomProvider(params double[] values) : IRandomProvider
{
    private readonly Queue<double> _values = new(values.Length == 0 ? [0.99] : values);

    public double NextDouble() => _values.Count > 0 ? _values.Dequeue() : 0.99;
}

public sealed class FakeEventBus : IEventBus
{
    public List<(string EventName, object Payload)> Published { get; } = [];

    public Task PublishAsync(string eventName, object payload, CancellationToken cancellationToken = default)
    {
        Published.Add((eventName, payload));
        return Task.CompletedTask;
    }
}

public sealed class FakeFileStorage : IFileStorage
{
    private readonly ConcurrentDictionary<string, string> _files = new();
    public List<(string Path, string Content)> Writes { get; } = [];
    public bool ThrowOnRead { get; set; }

    public Task<bool> ExistsAsync(string path, CancellationToken cancellationToken = default) =>
        Task.FromResult(_files.ContainsKey(path));

    public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
    {
        if (ThrowOnRead)
        {
            throw new InvalidOperationException("Corrupted file.");
        }

        return Task.FromResult(_files[path]);
    }

    public Task WriteAllTextAtomicAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        Writes.Add((path, content));
        _files[path] = content;
        return Task.CompletedTask;
    }

    public void Seed(string path, string content) => _files[path] = content;
    public string? Get(string path) => _files.TryGetValue(path, out var content) ? content : null;
}
