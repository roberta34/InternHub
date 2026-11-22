using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.GetCurrentUser;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/auth/me", async (
                ClaimsPrincipal principal,
                IGetCurrentUserHandler handler,
                CancellationToken ct) =>
            {
                var result = await handler.HandleAsync(
                    new GetCurrentUserQuery(
                        principal.FindFirst("sub")?.Value,
                        principal.FindFirst("role")?.Value,
                        principal.FindFirst("student_id")?.Value,
                        principal.FindFirst("recruiter_id")?.Value),
                    ct);

                return result.Status == ResultStatus.Success
                    ? Results.Ok(result.Value)
                    : result.ToFailure();
            })
            .RequireAuthorization();
}
