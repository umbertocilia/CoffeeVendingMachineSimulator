namespace CoffeeMachine.Domain;

public sealed class Boiler
{
    public double CurrentTemperature { get; set; } = 22;
    public double TargetTemperature { get; set; } = 90;
    public bool HeatingFaultActive { get; set; }
    public bool OverheatProtectionTriggered { get; set; }
    public DateTimeOffset? HeatingStartedAtUtc { get; set; }

    public Boiler Clone() => new()
    {
        CurrentTemperature = CurrentTemperature,
        TargetTemperature = TargetTemperature,
        HeatingFaultActive = HeatingFaultActive,
        OverheatProtectionTriggered = OverheatProtectionTriggered,
        HeatingStartedAtUtc = HeatingStartedAtUtc
    };
}

public sealed class WaterTank
{
    public string Id { get; set; } = "water-main";
    public double CapacityMl { get; set; } = 5000;
    public double CurrentLevelMl { get; set; } = 5000;
    public double LowLevelThresholdMl { get; set; } = 750;

    public WaterTank Clone() => new()
    {
        Id = Id,
        CapacityMl = CapacityMl,
        CurrentLevelMl = CurrentLevelMl,
        LowLevelThresholdMl = LowLevelThresholdMl
    };
}

public sealed class IngredientContainer
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = "g";
    public double Capacity { get; set; }
    public double CurrentLevel { get; set; }
    public double LowLevelThreshold { get; set; }
    public bool Enabled { get; set; } = true;

    public IngredientContainer Clone() => new()
    {
        Id = Id,
        Name = Name,
        Unit = Unit,
        Capacity = Capacity,
        CurrentLevel = CurrentLevel,
        LowLevelThreshold = LowLevelThreshold,
        Enabled = Enabled
    };
}

public sealed class DispensingUnit
{
    public bool IsBusy { get; set; }
    public bool FaultActive { get; set; }
    public string? CurrentOrderId { get; set; }

    public DispensingUnit Clone() => new()
    {
        IsBusy = IsBusy,
        FaultActive = FaultActive,
        CurrentOrderId = CurrentOrderId
    };
}

public sealed class CreditTransaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTimeOffset TimestampUtc { get; set; }
    public decimal Amount { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public CreditTransaction Clone() => new()
    {
        Id = Id,
        TimestampUtc = TimestampUtc,
        Amount = Amount,
        Type = Type,
        Description = Description
    };
}

public sealed class CreditAccount
{
    public decimal CurrentCredit { get; set; }
    public List<CreditTransaction> Transactions { get; set; } = [];

    public CreditAccount Clone() => new()
    {
        CurrentCredit = CurrentCredit,
        Transactions = Transactions.Select(x => x.Clone()).ToList()
    };
}

public sealed class Product
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string RecipeId { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public bool Enabled { get; set; } = true;

    public Product Clone() => new()
    {
        Id = Id,
        Name = Name,
        RecipeId = RecipeId,
        Price = Price,
        Enabled = Enabled
    };
}

public sealed class RecipeStep
{
    public int Sequence { get; set; }
    public string IngredientKey { get; set; } = string.Empty;
    public double Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int DurationMs { get; set; }

    public RecipeStep Clone() => new()
    {
        Sequence = Sequence,
        IngredientKey = IngredientKey,
        Quantity = Quantity,
        Unit = Unit,
        DurationMs = DurationMs
    };
}

public sealed class Recipe
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public double TargetTemperature { get; set; } = 90;
    public List<RecipeStep> Steps { get; set; } = [];

    public Recipe Clone() => new()
    {
        Id = Id,
        Name = Name,
        TargetTemperature = TargetTemperature,
        Steps = Steps.Select(x => x.Clone()).ToList()
    };
}

public sealed class Order
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string RecipeId { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public int CurrentStepIndex { get; set; }
    public int CurrentStepElapsedMs { get; set; }
    public double ProgressPercentage { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }

    public Order Clone() => new()
    {
        Id = Id,
        ProductId = ProductId,
        ProductName = ProductName,
        RecipeId = RecipeId,
        Status = Status,
        CurrentStepIndex = CurrentStepIndex,
        CurrentStepElapsedMs = CurrentStepElapsedMs,
        ProgressPercentage = ProgressPercentage,
        FailureReason = FailureReason,
        CreatedAtUtc = CreatedAtUtc,
        UpdatedAtUtc = UpdatedAtUtc,
        CompletedAtUtc = CompletedAtUtc
    };
}

public sealed class DiagnosticRecord
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DiagnosticSeverity Severity { get; set; }
    public bool Active { get; set; } = true;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? ResolvedAtUtc { get; set; }

    public DiagnosticRecord Clone() => new()
    {
        Code = Code,
        Message = Message,
        Severity = Severity,
        Active = Active,
        CreatedAtUtc = CreatedAtUtc,
        ResolvedAtUtc = ResolvedAtUtc
    };
}

public sealed class EventLogEntry
{
    public DateTimeOffset TimestampUtc { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public EventLogEntry Clone() => new()
    {
        TimestampUtc = TimestampUtc,
        Category = Category,
        Message = Message
    };
}

public sealed class MaintenanceInfo
{
    public int DispenseCount { get; set; }
    public double WearPercentage { get; set; }
    public int MaintenanceThreshold { get; set; } = 50;
    public bool MaintenanceRequired { get; set; }
    public DateTimeOffset? LastMaintenanceAtUtc { get; set; }

    public MaintenanceInfo Clone() => new()
    {
        DispenseCount = DispenseCount,
        WearPercentage = WearPercentage,
        MaintenanceThreshold = MaintenanceThreshold,
        MaintenanceRequired = MaintenanceRequired,
        LastMaintenanceAtUtc = LastMaintenanceAtUtc
    };
}

public sealed class MetricsState
{
    public int TotalOrders { get; set; }
    public int CompletedOrders { get; set; }
    public int FailedOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalCreditInserted { get; set; }
    public int SnapshotSaveCount { get; set; }
    public int SnapshotRestoreCount { get; set; }
    public DateTimeOffset? LastSimulationTickUtc { get; set; }

    public MetricsState Clone() => new()
    {
        TotalOrders = TotalOrders,
        CompletedOrders = CompletedOrders,
        FailedOrders = FailedOrders,
        CancelledOrders = CancelledOrders,
        TotalCreditInserted = TotalCreditInserted,
        SnapshotSaveCount = SnapshotSaveCount,
        SnapshotRestoreCount = SnapshotRestoreCount,
        LastSimulationTickUtc = LastSimulationTickUtc
    };
}

public sealed class SensorState
{
    public double WaterLevelPercentage { get; set; }
    public double BoilerTemperature { get; set; }
    public double AverageIngredientPercentage { get; set; }

    public SensorState Clone() => new()
    {
        WaterLevelPercentage = WaterLevelPercentage,
        BoilerTemperature = BoilerTemperature,
        AverageIngredientPercentage = AverageIngredientPercentage
    };
}

public sealed class SimulationConfig
{
    public int TickIntervalMs { get; set; } = 500;
    public double HeatingRatePerTick { get; set; } = 3;
    public double CoolingRatePerTick { get; set; } = 1.2;
    public int HeatingTimeoutSeconds { get; set; } = 90;
    public double ProcessFailureProbability { get; set; } = 0.02;
    public double MaximumBoilerTemperature { get; set; } = 98;
    public bool AutoSaveEnabled { get; set; } = true;
    public int AutoSaveIntervalSeconds { get; set; } = 15;

    public SimulationConfig Clone() => new()
    {
        TickIntervalMs = TickIntervalMs,
        HeatingRatePerTick = HeatingRatePerTick,
        CoolingRatePerTick = CoolingRatePerTick,
        HeatingTimeoutSeconds = HeatingTimeoutSeconds,
        ProcessFailureProbability = ProcessFailureProbability,
        MaximumBoilerTemperature = MaximumBoilerTemperature,
        AutoSaveEnabled = AutoSaveEnabled,
        AutoSaveIntervalSeconds = AutoSaveIntervalSeconds
    };
}

public sealed class CoffeeMachine
{
    public string Id { get; set; } = "machine-1";
    public MachineStatus Status { get; set; } = MachineStatus.Off;
    public bool PowerOn { get; set; }
    public bool PowerFaultActive { get; set; }
    public Boiler Boiler { get; set; } = new();
    public WaterTank WaterTank { get; set; } = new();
    public List<IngredientContainer> Ingredients { get; set; } = [];
    public DispensingUnit DispensingUnit { get; set; } = new();
    public CreditAccount Credit { get; set; } = new();
    public List<Product> Products { get; set; } = [];
    public List<Recipe> Recipes { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
    public List<DiagnosticRecord> Diagnostics { get; set; } = [];
    public List<EventLogEntry> RecentEvents { get; set; } = [];
    public MaintenanceInfo Maintenance { get; set; } = new();
    public MetricsState Metrics { get; set; } = new();
    public SensorState Sensors { get; set; } = new();
    public SimulationConfig SimulationConfig { get; set; } = new();
    public string SnapshotVersion { get; set; } = "1.0";

    public CoffeeMachine Clone() => new()
    {
        Id = Id,
        Status = Status,
        PowerOn = PowerOn,
        PowerFaultActive = PowerFaultActive,
        Boiler = Boiler.Clone(),
        WaterTank = WaterTank.Clone(),
        Ingredients = Ingredients.Select(x => x.Clone()).ToList(),
        DispensingUnit = DispensingUnit.Clone(),
        Credit = Credit.Clone(),
        Products = Products.Select(x => x.Clone()).ToList(),
        Recipes = Recipes.Select(x => x.Clone()).ToList(),
        Orders = Orders.Select(x => x.Clone()).ToList(),
        Diagnostics = Diagnostics.Select(x => x.Clone()).ToList(),
        RecentEvents = RecentEvents.Select(x => x.Clone()).ToList(),
        Maintenance = Maintenance.Clone(),
        Metrics = Metrics.Clone(),
        Sensors = Sensors.Clone(),
        SimulationConfig = SimulationConfig.Clone(),
        SnapshotVersion = SnapshotVersion
    };
}
