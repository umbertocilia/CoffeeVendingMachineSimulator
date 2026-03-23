using System.Threading;
using CoffeeMachine.Application;
using MachineState = CoffeeMachine.Domain.CoffeeMachine;

namespace CoffeeMachine.Infrastructure.Runtime;

public sealed class InMemoryCoffeeMachineRuntimeRepository : ICoffeeMachineRuntimeRepository
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private MachineState _machine;

    public InMemoryCoffeeMachineRuntimeRepository(MachineState seed)
    {
        _machine = seed;
    }

    public async Task<T> ReadAsync<T>(Func<MachineState, T> reader, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return reader(_machine);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<T> WriteAsync<T>(Func<MachineState, T> writer, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return writer(_machine);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task ReplaceAsync(MachineState machine, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            _machine = machine;
        }
        finally
        {
            _gate.Release();
        }
    }
}
