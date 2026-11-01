using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Tests.Unit.InternshipCandidacies.Fakes;

internal sealed class FakeInternshipCandidacyRepository : IInternshipCandidacyRepository
{
    private readonly List<InternshipCandidacy> _candidacies = [];

    public bool AddResult { get; set; } = true;
    public int AddCallCount { get; private set; }
    public int UpdateCallCount { get; private set; }
    public IReadOnlyList<InternshipCandidacy> Candidacies => _candidacies;

    public void Seed(InternshipCandidacy candidacy) =>
        _candidacies.Add(candidacy);

    public bool Add(InternshipCandidacy candidacy)
    {
        AddCallCount++;

        if (!AddResult || _candidacies.Any(existing => existing.Id == candidacy.Id))
            return false;

        _candidacies.Add(candidacy);
        return true;
    }

    public InternshipCandidacy? GetById(Guid id) =>
        _candidacies.SingleOrDefault(candidacy => candidacy.Id == id);

    public IReadOnlyList<InternshipCandidacy> GetByStudent(Guid studentId) =>
        _candidacies.Where(candidacy => candidacy.StudentId == studentId).ToArray();

    public IReadOnlyList<InternshipCandidacy> GetByOffer(Guid offerId) =>
        _candidacies.Where(candidacy => candidacy.OfferId == offerId).ToArray();

    public bool HasActiveForStudentAndOffer(Guid studentId, Guid offerId) =>
        _candidacies.Any(candidacy =>
            candidacy.StudentId == studentId &&
            candidacy.OfferId == offerId &&
            IsActive(candidacy.Status));

    public int CountActiveForStudent(Guid studentId) =>
        _candidacies.Count(candidacy =>
            candidacy.StudentId == studentId &&
            IsActive(candidacy.Status));

    public int CountAcceptedForOffer(Guid offerId) =>
        _candidacies.Count(candidacy =>
            candidacy.OfferId == offerId &&
            candidacy.Status == InternshipCandidacyStatus.Accepted);

    public bool Update(InternshipCandidacy candidacy)
    {
        UpdateCallCount++;

        var index = _candidacies.FindIndex(existing => existing.Id == candidacy.Id);
        if (index < 0)
            return false;

        _candidacies[index] = candidacy;
        return true;
    }

    private static bool IsActive(InternshipCandidacyStatus status) =>
        status is InternshipCandidacyStatus.Submitted
            or InternshipCandidacyStatus.Shortlisted
            or InternshipCandidacyStatus.Accepted
            or InternshipCandidacyStatus.Waitlisted;
}
