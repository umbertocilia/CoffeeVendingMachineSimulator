using CoffeeMachine.Infrastructure.Persistence;
using CoffeeMachine.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CoffeeMachine.Tests.Infrastructure;

public sealed class PersistenceTests
{
    [Fact]
    public async Task Restore_Should_Fallback_To_Seed_When_File_Is_Absent()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var service = CreateService(repository, new FakeFileStorage());

        var result = await service.RestoreAsync();
        var machineId = await repository.ReadAsync(machine => machine.Id);

        result.FallbackToSeed.Should().BeTrue();
        machineId.Should().Be("office-coffee-machine");
    }

    [Fact]
    public async Task Restore_Should_Fallback_To_Seed_When_File_Is_Corrupted()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var storage = new FakeFileStorage { ThrowOnRead = true };
        storage.Seed("state.json", "broken");
        var service = CreateService(repository, storage);

        var result = await service.RestoreAsync();

        result.FallbackToSeed.Should().BeTrue();
    }

    [Fact]
    public async Task Restore_Should_Reject_Unsupported_Snapshot_Version()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var storage = new FakeFileStorage();
        var serializer = TestFixtureFactory.CreateSerializer();
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);
        machine.SnapshotVersion = "0.9";
        storage.Seed("state.json", serializer.Serialize(machine));
        var service = CreateService(repository, storage);

        var result = await service.RestoreAsync();

        result.FallbackToSeed.Should().BeTrue();
        result.Message.Should().Contain("Invalid snapshot");
    }

    [Fact]
    public async Task Save_Should_Write_Snapshot_To_Configured_Path()
    {
        var repository = TestFixtureFactory.CreateRepository();
        var storage = new FakeFileStorage();
        var service = CreateService(repository, storage);

        await service.SaveAsync();

        storage.Writes.Should().ContainSingle(x => x.Path == "state.json");
        storage.Get("state.json").Should().Contain("\"snapshotVersion\": \"1.0\"");
    }

    [Fact]
    public async Task FileStorage_Should_Write_Atomically_Without_Leaving_Temp_File()
    {
        var storage = new FileStorage();
        var root = Path.Combine(Path.GetTempPath(), $"cm-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var path = Path.Combine(root, "state.json");

        await storage.WriteAllTextAtomicAsync(path, "{ \"ok\": true }");

        File.Exists(path).Should().BeTrue();
        File.Exists($"{path}.tmp").Should().BeFalse();
    }

    private static JsonStatePersistenceService CreateService(CoffeeMachine.Infrastructure.Runtime.InMemoryCoffeeMachineRuntimeRepository repository, FakeFileStorage storage)
        => new(
            repository,
            storage,
            TestFixtureFactory.CreateSerializer(),
            new FakeClock(),
            new FakeEventBus(),
            NullLogger<JsonStatePersistenceService>.Instance,
            Options.Create(new PersistenceOptions { SnapshotPath = "state.json" }));
}
