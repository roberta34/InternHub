namespace InternHub.Api.Features.Shared;

public enum ResultStatus
{
    Success,
    Validation,
    NotFound,
    Conflict
}

public sealed class Result<T>
{
    private Result(ResultStatus status, T? value, string? error)
    {
        Status = status;
        Value = value;
        Error = error;
    }

    public ResultStatus Status { get; }
    public T? Value { get; }
    public string? Error { get; }

    public static Result<T> Success(T value) => new(ResultStatus.Success, value, null);
    public static Result<T> Validation(string error) => new(ResultStatus.Validation, default, error);
    public static Result<T> NotFound(string error) => new(ResultStatus.NotFound, default, error);
    public static Result<T> Conflict(string error) => new(ResultStatus.Conflict, default, error);
}
