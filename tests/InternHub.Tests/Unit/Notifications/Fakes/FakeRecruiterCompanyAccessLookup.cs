using InternHub.Api.Features.Notifications.GetRecruiterNotifications;

namespace InternHub.Tests.Unit.Notifications.Fakes;

public sealed class FakeRecruiterCompanyAccessLookup : IRecruiterCompanyAccessLookup
{
    private readonly Dictionary<Guid, Guid> _recruiterCompanies = [];
    private readonly Dictionary<Guid, List<Guid>> _companyOffers = [];

    public int RecruiterCompanyLookupCalls { get; private set; }
    public int CompanyOfferLookupCalls { get; private set; }
    public Guid? LastQueriedRecruiterId { get; private set; }
    public Guid? LastQueriedCompanyId { get; private set; }
    public CancellationToken LastRecruiterLookupToken { get; private set; }
    public CancellationToken LastCompanyOffersLookupToken { get; private set; }

    public void AssignRecruiterToCompany(Guid recruiterId, Guid companyId) =>
        _recruiterCompanies[recruiterId] = companyId;

    public void AssignOffersToCompany(Guid companyId, params Guid[] offerIds) =>
        _companyOffers[companyId] = [.. offerIds];

    public Task<Guid?> GetRecruiterCompanyIdAsync(
        Guid recruiterId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RecruiterCompanyLookupCalls++;
        LastQueriedRecruiterId = recruiterId;
        LastRecruiterLookupToken = cancellationToken;

        return Task.FromResult<Guid?>(
            _recruiterCompanies.TryGetValue(recruiterId, out var companyId)
                ? companyId
                : null);
    }

    public Task<IReadOnlyList<Guid>> GetCompanyOfferIdsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CompanyOfferLookupCalls++;
        LastQueriedCompanyId = companyId;
        LastCompanyOffersLookupToken = cancellationToken;

        IReadOnlyList<Guid> offerIds = _companyOffers.TryGetValue(companyId, out var offers)
            ? offers.ToArray()
            : [];

        return Task.FromResult(offerIds);
    }
}
