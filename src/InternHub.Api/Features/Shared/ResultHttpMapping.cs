namespace InternHub.Api.Features.Shared;

public static class ResultHttpMapping
{
    public static IResult ToFailure<T>(this Result<T> result) => result.Status switch
    {
        ResultStatus.Validation => Results.BadRequest(result.Error),
        ResultStatus.NotFound => Results.NotFound(result.Error),
        ResultStatus.Conflict => Results.Conflict(result.Error),
        ResultStatus.Unauthorized => Results.Json(result.Error, statusCode: StatusCodes.Status401Unauthorized),
        ResultStatus.Forbidden => Results.Json(result.Error, statusCode: StatusCodes.Status403Forbidden),
        ResultStatus.ServiceUnavailable => Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            detail: result.Error),
        _ => throw new InvalidOperationException("Success is not a failure.")
    };
}
