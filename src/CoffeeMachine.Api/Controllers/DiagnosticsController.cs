using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class DiagnosticsController(
    IDiagnosticsApplicationService diagnosticsService,
    IStateApplicationService stateService) : ControllerBase
{
    [HttpGet("logs/recent")]
    public Task<IReadOnlyCollection<string>> GetRecentLogs([FromQuery] int lines = 100, CancellationToken cancellationToken = default) =>
        diagnosticsService.GetRecentLogsAsync(lines, cancellationToken);

    [HttpGet("errors/active")]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.DiagnosticRecord>> GetErrors(CancellationToken cancellationToken) =>
        diagnosticsService.GetActiveErrorsAsync(cancellationToken);

    [HttpGet("warnings/active")]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.DiagnosticRecord>> GetWarnings(CancellationToken cancellationToken) =>
        diagnosticsService.GetActiveWarningsAsync(cancellationToken);

    [HttpGet("maintenance/status")]
    public Task<CoffeeMachine.Domain.MaintenanceInfo> GetMaintenanceStatus(CancellationToken cancellationToken) =>
        diagnosticsService.GetMaintenanceStatusAsync(cancellationToken);

    [HttpGet("metrics")]
    public Task<CoffeeMachine.Domain.MetricsState> GetMetrics(CancellationToken cancellationToken) =>
        diagnosticsService.GetMetricsAsync(cancellationToken);

    [HttpPost("faults/inject")]
    public async Task<IActionResult> InjectFault([FromBody] FaultInjectionRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await diagnosticsService.InjectFaultAsync(request, cancellationToken));

    [HttpPost("state/save")]
    public async Task<IActionResult> SaveState(CancellationToken cancellationToken) =>
        this.FromResult(await stateService.SaveAsync(cancellationToken));

    [HttpPost("state/reload")]
    public async Task<IActionResult> ReloadState(CancellationToken cancellationToken) =>
        this.FromResult(await stateService.ReloadAsync(cancellationToken));

    [HttpGet("state/export")]
    public Task<string> ExportState(CancellationToken cancellationToken) =>
        stateService.ExportAsync(cancellationToken);
}
