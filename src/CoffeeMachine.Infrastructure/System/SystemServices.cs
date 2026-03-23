using CoffeeMachine.Application;

namespace CoffeeMachine.Infrastructure.System;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}

public sealed class RandomProvider : IRandomProvider
{
    private readonly Random _random = new();

    public double NextDouble() => _random.NextDouble();
}
