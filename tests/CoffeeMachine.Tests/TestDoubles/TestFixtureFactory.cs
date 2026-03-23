using CoffeeMachine.Application;
using CoffeeMachine.Infrastructure.Persistence;
using CoffeeMachine.Infrastructure.Runtime;

namespace CoffeeMachine.Tests.TestDoubles;

public static class TestFixtureFactory
{
    public static InMemoryCoffeeMachineRuntimeRepository CreateRepository() =>
        new(MachineSeedFactory.Create(new DateTimeOffset(2026, 03, 21, 10, 00, 00, TimeSpan.Zero)));

    public static JsonStateSnapshotSerializer CreateSerializer() => new();
}
