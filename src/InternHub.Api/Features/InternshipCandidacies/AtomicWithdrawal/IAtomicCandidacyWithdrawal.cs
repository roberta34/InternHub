namespace InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;

public interface IAtomicCandidacyWithdrawal
{
    Task<AtomicCandidacyWithdrawalResult> WithdrawAsync(
        Guid candidacyId,
        CancellationToken cancellationToken = default);
}
