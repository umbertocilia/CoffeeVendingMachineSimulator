using CoffeeMachine.Domain;

namespace CoffeeMachine.Application;

public sealed record MachineStatusDto(
    string MachineId,
    MachineStatus Status,
    bool PowerOn,
    double CurrentTemperature,
    double TargetTemperature,
    double WaterLevelMl,
    decimal CurrentCredit,
    bool MaintenanceRequired,
    IReadOnlyCollection<string> ActiveErrors,
    IReadOnlyCollection<string> ActiveWarnings);

public sealed record MachineComponentsDto(
    Boiler Boiler,
    WaterTank WaterTank,
    IReadOnlyCollection<IngredientContainer> Ingredients,
    DispensingUnit DispensingUnit,
    SensorState Sensors);

public sealed record DiagnosticsDto(
    IReadOnlyCollection<DiagnosticRecord> ActiveErrors,
    IReadOnlyCollection<DiagnosticRecord> ActiveWarnings,
    IReadOnlyCollection<EventLogEntry> RecentEvents,
    MetricsState Metrics,
    MaintenanceInfo Maintenance);

public sealed record CreditDto(decimal CurrentCredit);
public sealed record ProductUpsertRequest(string Name, string RecipeId, decimal Price, bool Enabled);
public sealed record RecipeStepRequest(int Sequence, string IngredientKey, double Quantity, string Unit, int DurationMs);
public sealed record RecipeUpsertRequest(string Name, double TargetTemperature, IReadOnlyCollection<RecipeStepRequest> Steps);
public sealed record IngredientUpdateRequest(string Name, string Unit, double Capacity, double CurrentLevel, double LowLevelThreshold, bool Enabled);
public sealed record RefillIngredientRequest(double Quantity);
public sealed record AddCreditRequest(decimal Amount, string Description);
public sealed record CreateOrderRequest(string ProductId);
public sealed record FaultInjectionRequest(FaultType FaultType, string? Message);
public sealed record ConfigDto(string MachineId, int MaintenanceThreshold);
public sealed record SimulationConfigDto(
    int TickIntervalMs,
    double HeatingRatePerTick,
    double CoolingRatePerTick,
    int HeatingTimeoutSeconds,
    double ProcessFailureProbability,
    double MaximumBoilerTemperature,
    bool AutoSaveEnabled,
    int AutoSaveIntervalSeconds);
