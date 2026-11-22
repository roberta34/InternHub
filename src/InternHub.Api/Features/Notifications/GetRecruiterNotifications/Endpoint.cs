using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/recruiters/me/notifications", async (
            ClaimsPrincipal user,
            IGetRecruiterNotificationsHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new GetRecruiterNotificationsQuery(user),
                cancellationToken);

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
