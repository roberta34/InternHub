using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

namespace InternHub.Api.Features.InternshipCandidacies.AtomicDecision;

public interface IAtomicCandidacyDecision
{
    Task<AtomicCandidacyDecisionResult> DecideAsync(
        Guid candidacyId,
        CandidacyDecision decision,
        CancellationToken cancellationToken = default
    );
}
