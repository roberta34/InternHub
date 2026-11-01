using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Tests.Unit.InternshipCandidacies.Fakes;

internal sealed class FakeInternshipCandidacyRepository : IInternshipCandidacyRepository
{
    public List<InternshipCandidacy> Candidacies { get; } = [];
    public int UpdateCallCount { get; private set; }

    public bool Add(InternshipCandidacy candidacy)
    {
        Candidacies.Add(candidacy);
        return true;
    }

    public InternshipCandidacy? GetById(Guid id) =>
        Candidacies.SingleOrDefault(candidacy => candidacy.Id == id);

    public IReadOnlyList<InternshipCandidacy> GetByStudent(Guid studentId) =>
        Candidacies.Where(candidacy => candidacy.StudentId == studentId).ToArray();

    public IReadOnlyList<InternshipCandidacy> GetByOffer(Guid offerId) =>
        Candidacies.Where(candidacy => candidacy.OfferId == offerId).ToArray();

    public bool HasActiveForStudentAndOffer(Guid studentId, Guid offerId) =>
        Candidacies.Any(candidacy =>
            candidacy.StudentId == studentId &&
            candidacy.OfferId == offerId &&
            candidacy.Status is InternshipCandidacyStatus.Submitted
                or InternshipCandidacyStatus.Shortlisted
                or InternshipCandidacyStatus.Accepted
                or InternshipCandidacyStatus.Waitlisted);

    public int CountActiveForStudent(Guid studentId) =>
        Candidacies.Count(candidacy =>
            candidacy.StudentId == studentId &&
            candidacy.Status is InternshipCandidacyStatus.Submitted
                or InternshipCandidacyStatus.Shortlisted
                or InternshipCandidacyStatus.Accepted
                or InternshipCandidacyStatus.Waitlisted);

    public int CountAcceptedForOffer(Guid offerId) =>
        Candidacies.Count(candidacy =>
            candidacy.OfferId == offerId &&
            candidacy.Status == InternshipCandidacyStatus.Accepted);

    public bool Update(InternshipCandidacy candidacy)
    {
        UpdateCallCount++;
        return Candidacies.Any(existing => existing.Id == candidacy.Id);
    }
}