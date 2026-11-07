using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;

public enum AtomicCandidacySubmissionStatus
{
    Created,
    StudentNotFound,
    OfferNotFound,
    OfferClosed,
    DeadlineExpired,
    DuplicateActive,
    ActiveLimitReached
}

public sealed class AtomicCandidacySubmissionResult
{
    private AtomicCandidacySubmissionResult(
        AtomicCandidacySubmissionStatus status,
        InternshipCandidacy? candidacy)
    {
        Status = status;
        Candidacy = candidacy;
    }

    public AtomicCandidacySubmissionStatus Status { get; }

    public InternshipCandidacy? Candidacy { get; }

    public static AtomicCandidacySubmissionResult Success(
        InternshipCandidacy candidacy)
    {
        ArgumentNullException.ThrowIfNull(candidacy);

        return new AtomicCandidacySubmissionResult(
            AtomicCandidacySubmissionStatus.Created,
            candidacy);
    }

    public static AtomicCandidacySubmissionResult Failure(
        AtomicCandidacySubmissionStatus status)
    {
        if (!Enum.IsDefined(status) ||
            status == AtomicCandidacySubmissionStatus.Created)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new AtomicCandidacySubmissionResult(status, null);
    }
}