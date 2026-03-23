using CoffeeMachine.Application.Results;
using Microsoft.AspNetCore.Mvc;

namespace CoffeeMachine.Api.Controllers;

public static class ApiResults
{
    public static IActionResult FromResult(this ControllerBase controller, Result result)
        => result.IsSuccess ? controller.Ok(result) : controller.BadRequest(new { error = result.Error });

    public static IActionResult FromResult<T>(this ControllerBase controller, Result<T> result)
        => result.IsSuccess ? controller.Ok(result.Value) : controller.BadRequest(new { error = result.Error });
}
