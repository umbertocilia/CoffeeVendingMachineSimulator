using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api/machine")]
public sealed class MachineController(IMachineApplicationService service) : ControllerBase
{
    [HttpGet("status")]
    public Task<MachineStatusDto> GetStatus(CancellationToken cancellationToken) => service.GetStatusAsync(cancellationToken);

    [HttpGet("diagnostics")]
    public Task<DiagnosticsDto> GetDiagnostics(CancellationToken cancellationToken) => service.GetDiagnosticsAsync(cancellationToken);

    [HttpGet("components")]
    public Task<MachineComponentsDto> GetComponents(CancellationToken cancellationToken) => service.GetComponentsAsync(cancellationToken);

    [HttpPost("power/on")]
    public async Task<IActionResult> PowerOn(CancellationToken cancellationToken) => this.FromResult(await service.PowerOnAsync(cancellationToken));

    [HttpPost("power/off")]
    public async Task<IActionResult> PowerOff(CancellationToken cancellationToken) => this.FromResult(await service.PowerOffAsync(cancellationToken));

    [HttpPost("reset")]
    public async Task<IActionResult> Reset(CancellationToken cancellationToken) => this.FromResult(await service.ResetAsync(cancellationToken));

    [HttpPost("maintenance/reset")]
    public async Task<IActionResult> ResetMaintenance(CancellationToken cancellationToken) => this.FromResult(await service.ResetMaintenanceAsync(cancellationToken));
}
