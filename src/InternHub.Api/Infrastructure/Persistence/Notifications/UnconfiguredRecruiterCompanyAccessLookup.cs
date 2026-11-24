using InternHub.Api.Features.Notifications.GetRecruiterNotifications;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence.Notifications;

/// <summary>
/// Maps recruiter ownership through the persisted Offer.RecruiterId column.
/// </summary>
public sealed class UnconfiguredRecruiterCompanyAccessLookup(
    InternHubDbContext db) : IRecruiterCompanyAccessLookup
{
    public async Task<Guid?> GetRecruiterCompanyIdAsync(
        Guid recruiterId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (recruiterId == Guid.Empty)
            return null;

        var ownsOffer = await db.Offers
            .AsNoTracking()
            .AnyAsync(offer => offer.RecruiterId == recruiterId, cancellationToken);

        return ownsOffer ? recruiterId : null;
    }

    public async Task<IReadOnlyList<Guid>> GetCompanyOfferIdsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (companyId == Guid.Empty)
            return [];

        return await db.Offers
            .AsNoTracking()
            .Where(offer => offer.RecruiterId == companyId)
            .Select(offer => offer.Id)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
