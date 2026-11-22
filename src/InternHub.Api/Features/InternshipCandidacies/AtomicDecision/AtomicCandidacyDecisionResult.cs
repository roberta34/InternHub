using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.AtomicDecision;

public enum AtomicCandidacyDecisionStatus
{
    Decided,
    CandidacyNotFound,
    OfferNotFound,
    InvalidTransition,
    ConcurrencyConflict
}

public sealed class AtomicCandidacyDecisionResult
{
    private AtomicCandidacyDecisionResult(
        AtomicCandidacyDecisionStatus status,
        InternshipCandidacy? candidacy)
    {
        Status = status;
        Candidacy = candidacy;
    }

    public AtomicCandidacyDecisionStatus Status { get; }

    public InternshipCandidacy? Candidacy { get; }

    public static AtomicCandidacyDecisionResult Success(
        InternshipCandidacy candidacy)
    {
        ArgumentNullException.ThrowIfNull(candidacy);

        return new AtomicCandidacyDecisionResult(
            AtomicCandidacyDecisionStatus.Decided,
            candidacy);
    }

    public static AtomicCandidacyDecisionResult Failure(
        AtomicCandidacyDecisionStatus status)
    {
        if (!Enum.IsDefined(status) ||
            status == AtomicCandidacyDecisionStatus.Decided)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new AtomicCandidacyDecisionResult(status, null);
    }
}
