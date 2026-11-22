namespace InternHub.Api.Domain.Notifications;

public sealed class NotificationOutboxEntry
{
    public Guid Id { get; private set; }
    public Guid CandidacyId { get; private set; }
    public Guid OfferId { get; private set; }
    public Guid StudentId { get; private set; }
    public NotificationKind Kind { get; private set; }
    public DateTime OccurredAtUtc { get; private set; }

    public NotificationOutboxEntry(
        Guid id,
        Guid candidacyId,
        Guid offerId,
        Guid studentId,
        NotificationKind kind,
        DateTime occurredAtUtc)
    {
        if (id == Guid.Empty)
            throw new ArgumentException("Id must not be empty.", nameof(id));

        if (candidacyId == Guid.Empty)
            throw new ArgumentException("CandidacyId must not be empty.", nameof(candidacyId));

        if (offerId == Guid.Empty)
            throw new ArgumentException("OfferId must not be empty.", nameof(offerId));

        if (studentId == Guid.Empty)
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));

        if (!Enum.IsDefined(kind))
            throw new ArgumentOutOfRangeException(
                nameof(kind),
                kind,
                "Kind must be a defined NotificationKind value.");

        if (occurredAtUtc.Kind != DateTimeKind.Utc)
            throw new ArgumentException(
                "OccurredAtUtc must be a UTC timestamp.",
                nameof(occurredAtUtc));

        if (occurredAtUtc == default)
            throw new ArgumentException(
                "OccurredAtUtc must not be the default timestamp.",
                nameof(occurredAtUtc));

        Id = id;
        CandidacyId = candidacyId;
        OfferId = offerId;
        StudentId = studentId;
        Kind = kind;
        OccurredAtUtc = occurredAtUtc;
    }

    public static NotificationOutboxEntry ShortlistedCandidacyWithdrawn(
        Guid id,
        Guid candidacyId,
        Guid offerId,
        Guid studentId,
        DateTime occurredAtUtc) =>
        new(
            id,
            candidacyId,
            offerId,
            studentId,
            NotificationKind.ShortlistedCandidacyWithdrawn,
            occurredAtUtc);
}
