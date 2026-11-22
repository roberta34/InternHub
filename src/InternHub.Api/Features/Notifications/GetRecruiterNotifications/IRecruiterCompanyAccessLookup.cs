namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public interface IRecruiterCompanyAccessLookup
{
    Task<Guid?> GetRecruiterCompanyIdAsync(
        Guid recruiterId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Guid>> GetCompanyOfferIdsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);
}
