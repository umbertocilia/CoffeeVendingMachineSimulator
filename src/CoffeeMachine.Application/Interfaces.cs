using CoffeeMachine.Application.Results;
using CoffeeMachine.Domain;

namespace CoffeeMachine.Application;

public interface IMachineApplicationService
{
    Task<MachineStatusDto> GetStatusAsync(CancellationToken cancellationToken = default);
    Task<MachineComponentsDto> GetComponentsAsync(CancellationToken cancellationToken = default);
    Task<DiagnosticsDto> GetDiagnosticsAsync(CancellationToken cancellationToken = default);
    Task<Result> PowerOnAsync(CancellationToken cancellationToken = default);
    Task<Result> PowerOffAsync(CancellationToken cancellationToken = default);
    Task<Result> ResetAsync(CancellationToken cancellationToken = default);
    Task<Result> ResetMaintenanceAsync(CancellationToken cancellationToken = default);
}

public interface ICreditApplicationService
{
    Task<CreditDto> GetCreditAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<CreditTransaction>> GetTransactionsAsync(CancellationToken cancellationToken = default);
    Task<Result<CreditDto>> AddCreditAsync(AddCreditRequest request, CancellationToken cancellationToken = default);
    Task<Result<CreditDto>> ResetCreditAsync(CancellationToken cancellationToken = default);
}

public interface ICatalogApplicationService
{
    Task<IReadOnlyCollection<Product>> GetProductsAsync(CancellationToken cancellationToken = default);
    Task<Product?> GetProductAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<Product>> CreateProductAsync(ProductUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<Product>> UpdateProductAsync(string id, ProductUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteProductAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Recipe>> GetRecipesAsync(CancellationToken cancellationToken = default);
    Task<Recipe?> GetRecipeAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<Recipe>> CreateRecipeAsync(RecipeUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result<Recipe>> UpdateRecipeAsync(string id, RecipeUpsertRequest request, CancellationToken cancellationToken = default);
    Task<Result> DeleteRecipeAsync(string id, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<IngredientContainer>> GetIngredientsAsync(CancellationToken cancellationToken = default);
    Task<IngredientContainer?> GetIngredientAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<IngredientContainer>> UpdateIngredientAsync(string id, IngredientUpdateRequest request, CancellationToken cancellationToken = default);
    Task<Result<IngredientContainer>> RefillIngredientAsync(string id, RefillIngredientRequest request, CancellationToken cancellationToken = default);
    Task<WaterTank> GetWaterTankAsync(CancellationToken cancellationToken = default);
    Task<Result<WaterTank>> RefillWaterAsync(double quantityMl, CancellationToken cancellationToken = default);
}

public interface IOrderApplicationService
{
    Task<Result<Order>> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Order>> GetOrdersAsync(CancellationToken cancellationToken = default);
    Task<Order?> GetOrderAsync(string id, CancellationToken cancellationToken = default);
    Task<Result<Order>> CancelOrderAsync(string id, CancellationToken cancellationToken = default);
}

public interface IConfigApplicationService
{
    Task<ConfigDto> GetConfigAsync(CancellationToken cancellationToken = default);
    Task<Result<ConfigDto>> UpdateConfigAsync(ConfigDto request, CancellationToken cancellationToken = default);
    Task<SimulationConfigDto> GetSimulationConfigAsync(CancellationToken cancellationToken = default);
    Task<Result<SimulationConfigDto>> UpdateSimulationConfigAsync(SimulationConfigDto request, CancellationToken cancellationToken = default);
}

public interface IDiagnosticsApplicationService
{
    Task<IReadOnlyCollection<DiagnosticRecord>> GetActiveErrorsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<DiagnosticRecord>> GetActiveWarningsAsync(CancellationToken cancellationToken = default);
    Task<MaintenanceInfo> GetMaintenanceStatusAsync(CancellationToken cancellationToken = default);
    Task<MetricsState> GetMetricsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<string>> GetRecentLogsAsync(int lines, CancellationToken cancellationToken = default);
    Task<Result> InjectFaultAsync(FaultInjectionRequest request, CancellationToken cancellationToken = default);
}

public interface IStateApplicationService
{
    Task<Result> SaveAsync(CancellationToken cancellationToken = default);
    Task<Result<StateRestoreResult>> ReloadAsync(CancellationToken cancellationToken = default);
    Task<string> ExportAsync(CancellationToken cancellationToken = default);
}
