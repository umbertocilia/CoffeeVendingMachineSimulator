using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class CreditController(ICreditApplicationService service) : ControllerBase
{
    [HttpGet("credit")]
    public Task<CreditDto> GetCredit(CancellationToken cancellationToken) => service.GetCreditAsync(cancellationToken);

    [HttpPost("credit/add")]
    public async Task<IActionResult> AddCredit([FromBody] AddCreditRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.AddCreditAsync(request, cancellationToken));

    [HttpPost("credit/reset")]
    public async Task<IActionResult> ResetCredit(CancellationToken cancellationToken) =>
        this.FromResult(await service.ResetCreditAsync(cancellationToken));

    [HttpGet("transactions")]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.CreditTransaction>> GetTransactions(CancellationToken cancellationToken) =>
        service.GetTransactionsAsync(cancellationToken);
}
