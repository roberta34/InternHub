using InternHub.Api.Domain.Notifications;
using InternHub.Api.Features.Notifications.GetRecruiterNotifications;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence.Notifications;

public sealed class EfCoreNotificationOutboxReader(
    InternHubDbContext db) : INotificationOutboxReader
{
    public async Task<IReadOnlyList<NotificationOutboxEntry>> GetByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(offerIds);

        var distinctOfferIds = offerIds
            .Where(offerId => offerId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (distinctOfferIds.Length == 0)
        {
            return [];
        }

        return await db.NotificationOutboxEntries
            .AsNoTracking()
            .Where(entry => distinctOfferIds.Contains(entry.OfferId))
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenBy(entry => entry.Id)
            .ToListAsync(cancellationToken);
    }
}
