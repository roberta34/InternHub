using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.GetCurrentUser;

public interface IGetCurrentUserHandler
{
    Task<Result<GetCurrentUserResponse>> HandleAsync(
        GetCurrentUserQuery query,
        CancellationToken ct);
}
