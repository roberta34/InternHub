using InternHub.Api.Domain.Notifications;

namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public interface INotificationOutboxReader
{
    Task<IReadOnlyList<NotificationOutboxEntry>> GetByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken = default);
}
