using InternHub.Api.Domain.Notifications;

namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public sealed record GetRecruiterNotificationsResponse(
    Guid Id,
    Guid CandidacyId,
    Guid OfferId,
    Guid StudentId,
    NotificationKind Kind,
    DateTime OccurredAtUtc);
