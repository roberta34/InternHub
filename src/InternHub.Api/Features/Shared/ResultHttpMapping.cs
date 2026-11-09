namespace InternHub.Api.Features.Shared;

public static class ResultHttpMapping
{
    public static IResult ToFailure<T>(this Result<T> result) => result.Status switch
    {
        ResultStatus.Validation => Results.BadRequest(result.Error),
        ResultStatus.NotFound => Results.NotFound(result.Error),
        ResultStatus.Conflict => Results.Conflict(result.Error),
        ResultStatus.ServiceUnavailable => Results.Problem(
            statusCode: StatusCodes.Status503ServiceUnavailable,
            detail: result.Error),
        _ => throw new InvalidOperationException("Success is not a failure.")
    };
}
