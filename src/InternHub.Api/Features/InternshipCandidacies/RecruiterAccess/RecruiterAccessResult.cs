using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;

public enum RecruiterAccessStatus
{
    Authorized,
    Unauthorized,
    Forbidden
}

public sealed class RecruiterAccessResult
{
    private RecruiterAccessResult(
        RecruiterAccessStatus status,
        Guid? recruiterId,
        string? error)
    {
        Status = status;
        RecruiterId = recruiterId;
        Error = error;
    }

    public RecruiterAccessStatus Status { get; }
    public Guid? RecruiterId { get; }
    public string? Error { get; }
    public bool IsAuthorized => Status == RecruiterAccessStatus.Authorized;

    public static RecruiterAccessResult Authorized(Guid recruiterId)
    {
        if (recruiterId == Guid.Empty)
            throw new ArgumentException("RecruiterId must not be empty.", nameof(recruiterId));

        return new RecruiterAccessResult(RecruiterAccessStatus.Authorized, recruiterId, null);
    }

    public static RecruiterAccessResult Unauthorized(string error) =>
        new(RecruiterAccessStatus.Unauthorized, null, error);

    public static RecruiterAccessResult Forbidden(string error) =>
        new(RecruiterAccessStatus.Forbidden, null, error);

    /// <summary>Maps a denied access to the matching failed Result (401 or 403).</summary>
    public Result<T> ToFailure<T>()
    {
        if (IsAuthorized)
            throw new InvalidOperationException("An authorized access is not a failure.");

        return Failures<T>.ByStatus[Status](Error!);
    }

    private static class Failures<T>
    {
        public static readonly IReadOnlyDictionary<RecruiterAccessStatus, Func<string, Result<T>>> ByStatus =
            new Dictionary<RecruiterAccessStatus, Func<string, Result<T>>>
            {
                [RecruiterAccessStatus.Unauthorized] = Result<T>.Unauthorized,
                [RecruiterAccessStatus.Forbidden] = Result<T>.Forbidden
            };
    }
}
