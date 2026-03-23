namespace CoffeeMachine.Domain;

public enum MachineStatus
{
    Off,
    Initializing,
    Ready,
    Heating,
    Dispensing,
    OutOfService,
    MaintenanceRequired,
    Error
}

public enum OrderStatus
{
    Pending,
    Validating,
    WaitingForHeat,
    DispensingIngredient,
    Mixing,
    Completed,
    Failed,
    Cancelled
}

public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}

public enum FaultType
{
    None,
    PowerFailure,
    HeatingFailure,
    Overheat,
    WaterEmpty,
    IngredientDepleted,
    DispensingFailure,
    MaintenanceLock
}
