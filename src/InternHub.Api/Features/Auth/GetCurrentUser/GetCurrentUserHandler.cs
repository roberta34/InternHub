using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.GetCurrentUser;

public sealed class GetCurrentUserHandler : IGetCurrentUserHandler
{
    public Task<Result<GetCurrentUserResponse>> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken ct)
    {
        if (!Guid.TryParse(query.Subject, out var accountId)
            || string.IsNullOrWhiteSpace(query.Role)
            || !TryParseOptionalGuid(query.StudentId, out var studentId)
            || !TryParseOptionalGuid(query.RecruiterId, out var recruiterId))
        {
            return Task.FromResult(
                Result<GetCurrentUserResponse>.Unauthorized("The authenticated account claims are invalid."));
        }

        return Task.FromResult(Result<GetCurrentUserResponse>.Success(
            new GetCurrentUserResponse(accountId, query.Role, studentId, recruiterId)));
    }

    private static bool TryParseOptionalGuid(string? value, out Guid? parsed)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            parsed = null;
            return true;
        }

        if (Guid.TryParse(value, out var id))
        {
            parsed = id;
            return true;
        }

        parsed = null;
        return false;
    }
}
