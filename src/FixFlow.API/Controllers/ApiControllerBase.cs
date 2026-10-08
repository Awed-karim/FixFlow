using FixFlow.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace FixFlow.API.Controllers;

[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    protected string UserId => User.FindFirst("sub")?.Value ?? string.Empty;
    protected string UserRole => User.FindFirst("role")?.Value ?? string.Empty;

    protected IActionResult FromResult(Result result)
    {
        if (result.Succeeded)
            return NoContent();

        return ErrorResponse(result);
    }

    protected IActionResult FromResult<T>(Result<T> result)
    {
        if (result.Succeeded)
            return Ok(result.Data);

        return ErrorResponse(result);
    }

    protected IActionResult ErrorResponse(Result result)
    {
        var body = new { error = result.Error };

        return result.ErrorType switch
        {
            ErrorType.NotFound => NotFound(body),
            ErrorType.Forbidden => StatusCode(StatusCodes.Status403Forbidden, body),
            ErrorType.Conflict => Conflict(body),
            _ => BadRequest(body)
        };
    }
}