using System.Security.Claims;

namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public sealed record GetRecruiterNotificationsQuery(ClaimsPrincipal? User);
