namespace InternHub.Api.Domain.InternshipCandidacies;

public interface IInternshipCandidacyRepository
{
    bool Add(InternshipCandidacy candidacy);

    InternshipCandidacy? GetById(Guid id);

    IReadOnlyList<InternshipCandidacy> GetByStudent(Guid studentId);

    IReadOnlyList<InternshipCandidacy> GetByOffer(Guid offerId);

    bool HasActiveForStudentAndOffer(Guid studentId, Guid offerId);

    int CountActiveForStudent(Guid studentId);

    int CountAcceptedForOffer(Guid offerId);

    bool Update(InternshipCandidacy candidacy);
}