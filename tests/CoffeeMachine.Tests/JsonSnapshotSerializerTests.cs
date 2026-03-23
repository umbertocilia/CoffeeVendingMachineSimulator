using CoffeeMachine.Infrastructure.Persistence;
using FluentAssertions;

namespace CoffeeMachine.Tests;

public sealed class JsonSnapshotSerializerTests
{
    [Fact]
    public void Serializer_Should_RoundTrip_MachineState()
    {
        var serializer = new JsonStateSnapshotSerializer();
        var machine = MachineSeedFactory.Create(DateTimeOffset.UtcNow);

        var json = serializer.Serialize(machine);
        var restored = serializer.Deserialize(json);

        restored.Id.Should().Be(machine.Id);
        restored.Products.Should().HaveCount(machine.Products.Count);
        restored.Recipes.Should().HaveCount(machine.Recipes.Count);
    }
}
