using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api/config")]
public sealed class ConfigController(IConfigApplicationService service) : ControllerBase
{
    [HttpGet]
    public Task<ConfigDto> GetConfig(CancellationToken cancellationToken) => service.GetConfigAsync(cancellationToken);

    [HttpPut]
    public async Task<IActionResult> UpdateConfig([FromBody] ConfigDto request, CancellationToken cancellationToken) =>
        this.FromResult(await service.UpdateConfigAsync(request, cancellationToken));

    [HttpGet("simulation")]
    public Task<SimulationConfigDto> GetSimulation(CancellationToken cancellationToken) => service.GetSimulationConfigAsync(cancellationToken);

    [HttpPut("simulation")]
    public async Task<IActionResult> UpdateSimulation([FromBody] SimulationConfigDto request, CancellationToken cancellationToken) =>
        this.FromResult(await service.UpdateSimulationConfigAsync(request, cancellationToken));
}
