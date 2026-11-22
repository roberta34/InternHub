using InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public sealed class WithdrawInternshipCandidacyHandler(
    IAtomicCandidacyWithdrawal withdrawals) : IWithdrawInternshipCandidacyHandler
{
    private static readonly IReadOnlyDictionary<AtomicCandidacyWithdrawalStatus,
        Func<Guid, AtomicCandidacyWithdrawalResult, Result<bool>>> Outcomes =
        new Dictionary<AtomicCandidacyWithdrawalStatus,
            Func<Guid, AtomicCandidacyWithdrawalResult, Result<bool>>>
        {
            [AtomicCandidacyWithdrawalStatus.Withdrawn] = (_, _) =>
                Result<bool>.Success(true),
            [AtomicCandidacyWithdrawalStatus.CandidacyNotFound] = (id, _) =>
                Result<bool>.NotFound($"Candidacy {id} not found."),
            [AtomicCandidacyWithdrawalStatus.InvalidTransition] = (id, outcome) =>
                Result<bool>.Conflict(
                    $"Candidacy {id} cannot be withdrawn from status {outcome.CurrentStatus}."),
            [AtomicCandidacyWithdrawalStatus.ConcurrencyConflict] = (id, _) =>
                Result<bool>.Conflict(
                    $"Candidacy {id} could not be withdrawn due to a concurrent update. Retry the request.")
        };

    public async Task<Result<bool>> HandleAsync(
        WithdrawInternshipCandidacyCommand command,
        CancellationToken cancellationToken = default)
    {
        var outcome = await withdrawals.WithdrawAsync(command.CandidacyId, cancellationToken);

        return Outcomes[outcome.Status](command.CandidacyId, outcome);
    }
}
