using InternHub.Api.Domain.Notifications;
using InternHub.Api.Features.Notifications.GetRecruiterNotifications;

namespace InternHub.Tests.Unit.Notifications.Fakes;

public sealed class FakeNotificationOutboxReader : INotificationOutboxReader
{
    private readonly List<NotificationOutboxEntry> _entries = [];

    public int ReadCalls { get; private set; }
    public IReadOnlyList<Guid>? LastRequestedOfferIds { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }
    public IReadOnlyList<NotificationOutboxEntry> Entries => _entries.AsReadOnly();

    public void Seed(params NotificationOutboxEntry[] entries) =>
        _entries.AddRange(entries);

    public Task<IReadOnlyList<NotificationOutboxEntry>> GetByOfferIdsAsync(
        IReadOnlyCollection<Guid> offerIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ReadCalls++;
        LastRequestedOfferIds = offerIds.ToArray();
        LastCancellationToken = cancellationToken;

        var offerIdSet = offerIds.ToHashSet();
        IReadOnlyList<NotificationOutboxEntry> matching = _entries
            .Where(entry => offerIdSet.Contains(entry.OfferId))
            .ToArray();

        return Task.FromResult(matching);
    }
}
