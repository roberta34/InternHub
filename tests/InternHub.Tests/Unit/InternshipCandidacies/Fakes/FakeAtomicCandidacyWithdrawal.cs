using InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;

namespace InternHub.Tests.Unit.InternshipCandidacies.Fakes;

internal sealed class FakeAtomicCandidacyWithdrawal : IAtomicCandidacyWithdrawal
{
    public AtomicCandidacyWithdrawalResult Result { get; set; } =
        AtomicCandidacyWithdrawalResult.Failure(
            AtomicCandidacyWithdrawalStatus.CandidacyNotFound);

    public Exception? ExceptionToThrow { get; set; }

    public int CallCount { get; private set; }
    public Guid? LastCandidacyId { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<AtomicCandidacyWithdrawalResult> WithdrawAsync(
        Guid candidacyId,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastCandidacyId = candidacyId;
        LastCancellationToken = cancellationToken;

        if (ExceptionToThrow is not null)
        {
            return Task.FromException<AtomicCandidacyWithdrawalResult>(ExceptionToThrow);
        }

        return Task.FromResult(Result);
    }
}
