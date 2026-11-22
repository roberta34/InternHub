using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

namespace InternHub.Tests.Unit.InternshipCandidacies.Fakes;

internal sealed class FakeAtomicCandidacyDecision : IAtomicCandidacyDecision
{
    public AtomicCandidacyDecisionResult Result { get; set; } =
        AtomicCandidacyDecisionResult.Failure(AtomicCandidacyDecisionStatus.CandidacyNotFound);

    public int CallCount { get; private set; }
    public Guid? LastCandidacyId { get; private set; }
    public CandidacyDecision? LastDecision { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<AtomicCandidacyDecisionResult> DecideAsync(
        Guid candidacyId,
        CandidacyDecision decision,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastCandidacyId = candidacyId;
        LastDecision = decision;
        LastCancellationToken = cancellationToken;

        return Task.FromResult(Result);
    }
}
