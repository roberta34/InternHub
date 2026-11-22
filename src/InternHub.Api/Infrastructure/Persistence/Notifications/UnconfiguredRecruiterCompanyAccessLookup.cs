using InternHub.Api.Features.Notifications.GetRecruiterNotifications;

namespace InternHub.Api.Infrastructure.Persistence.Notifications;

/// <summary>
/// Fail-closed temporary lookup used until Company/Recruiter/Offer ownership
/// persistence from P3 is integrated. Never exposes notifications or fake data.
/// </summary>
public sealed class UnconfiguredRecruiterCompanyAccessLookup : IRecruiterCompanyAccessLookup
{
    public Task<Guid?> GetRecruiterCompanyIdAsync(
        Guid recruiterId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Guid?>(null);
    }

    public Task<IReadOnlyList<Guid>> GetCompanyOfferIdsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<IReadOnlyList<Guid>>([]);
    }
}
