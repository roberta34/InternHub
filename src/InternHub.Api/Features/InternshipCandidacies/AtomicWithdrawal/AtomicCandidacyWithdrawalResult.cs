using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;

public enum AtomicCandidacyWithdrawalStatus
{
    Withdrawn,
    CandidacyNotFound,
    InvalidTransition,
    ConcurrencyConflict
}

public sealed class AtomicCandidacyWithdrawalResult
{
    private AtomicCandidacyWithdrawalResult(
        AtomicCandidacyWithdrawalStatus status,
        InternshipCandidacy? candidacy,
        InternshipCandidacyStatus? currentStatus)
    {
        Status = status;
        Candidacy = candidacy;
        CurrentStatus = currentStatus;
    }

    public AtomicCandidacyWithdrawalStatus Status { get; }

    public InternshipCandidacy? Candidacy { get; }

    public InternshipCandidacyStatus? CurrentStatus { get; }

    public static AtomicCandidacyWithdrawalResult Success(
        InternshipCandidacy candidacy)
    {
        ArgumentNullException.ThrowIfNull(candidacy);

        return new AtomicCandidacyWithdrawalResult(
            AtomicCandidacyWithdrawalStatus.Withdrawn,
            candidacy,
            candidacy.Status);
    }

    public static AtomicCandidacyWithdrawalResult InvalidTransition(
        InternshipCandidacyStatus currentStatus)
    {
        if (!Enum.IsDefined(currentStatus))
        {
            throw new ArgumentOutOfRangeException(nameof(currentStatus));
        }

        return new AtomicCandidacyWithdrawalResult(
            AtomicCandidacyWithdrawalStatus.InvalidTransition,
            null,
            currentStatus);
    }

    public static AtomicCandidacyWithdrawalResult Failure(
        AtomicCandidacyWithdrawalStatus status)
    {
        if (!Enum.IsDefined(status) ||
            status == AtomicCandidacyWithdrawalStatus.Withdrawn ||
            status == AtomicCandidacyWithdrawalStatus.InvalidTransition)
        {
            throw new ArgumentOutOfRangeException(nameof(status));
        }

        return new AtomicCandidacyWithdrawalResult(status, null, null);
    }
}
