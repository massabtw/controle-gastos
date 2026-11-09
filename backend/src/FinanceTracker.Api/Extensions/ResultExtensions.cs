using FinanceTracker.Domain.Results;
using Microsoft.AspNetCore.Mvc;

namespace FinanceTracker.Api.Extensions;

public static class ResultExtensions
{
    public static IActionResult Ok<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);

        return HandleError(result);
    }

    public static IActionResult Ok(this Result result)
    {
        if (result.IsSuccess)
            return new OkResult();

        return HandleError(result);
    }

    public static IActionResult Created<T>(this Result<T> result, string uri = "")
    {
        if (result.IsSuccess)
            return new ObjectResult(result.Value) { StatusCode = StatusCodes.Status201Created };

        return HandleError(result);
    }

    public static IActionResult NoContent(this Result result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return HandleError(result);
    }

    public static IActionResult NoContent<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new NoContentResult();

        return HandleError(result);
    }

    private static IActionResult HandleError(Result result)
    {
        var errorObj = new { message = result.Error };

        return result.StatusCode switch
        {
            StatusCodes.Status401Unauthorized => new UnauthorizedObjectResult(errorObj),
            StatusCodes.Status404NotFound => new NotFoundObjectResult(errorObj),
            StatusCodes.Status409Conflict => new ConflictObjectResult(errorObj),
            StatusCodes.Status403Forbidden => new ObjectResult(errorObj) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new BadRequestObjectResult(errorObj)
        };
    }
}
