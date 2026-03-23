using CoffeeMachine.Application;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

[ApiController]
[Route("api/orders")]
public sealed class OrdersController(IOrderApplicationService service) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request, CancellationToken cancellationToken) =>
        this.FromResult(await service.CreateOrderAsync(request, cancellationToken));

    [HttpGet]
    public Task<IReadOnlyCollection<CoffeeMachine.Domain.Order>> GetAll(CancellationToken cancellationToken) =>
        service.GetOrdersAsync(cancellationToken);

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id, CancellationToken cancellationToken)
    {
        var order = await service.GetOrderAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(order);
    }

    [HttpGet("{id}/progress")]
    public async Task<IActionResult> GetProgress(string id, CancellationToken cancellationToken)
    {
        var order = await service.GetOrderAsync(id, cancellationToken);
        return order is null ? NotFound() : Ok(new { order.Id, order.Status, order.ProgressPercentage, order.CurrentStepIndex, order.FailureReason });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> Cancel(string id, CancellationToken cancellationToken) =>
        this.FromResult(await service.CancelOrderAsync(id, cancellationToken));
}
