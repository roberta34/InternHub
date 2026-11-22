using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public interface IGetRecruiterNotificationsHandler
{
    Task<Result<IReadOnlyList<GetRecruiterNotificationsResponse>>> HandleAsync(
        GetRecruiterNotificationsQuery query,
        CancellationToken cancellationToken = default);
}
