using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public sealed class DecideInternshipCandidacyHandler(
    IAtomicCandidacyDecision decisions) : IDecideInternshipCandidacyHandler
{
    private static readonly IReadOnlyDictionary<CandidacyDecision, string> Verbs =
        new Dictionary<CandidacyDecision, string>
        {
            [CandidacyDecision.Shortlist] = "shortlisted",
            [CandidacyDecision.Accept] = "accepted",
            [CandidacyDecision.Reject] = "rejected"
        };

    private static readonly IReadOnlyDictionary<AtomicCandidacyDecisionStatus,
        Func<Guid, CandidacyDecision, AtomicCandidacyDecisionResult, Result<DecideInternshipCandidacyResponse>>> Outcomes =
        new Dictionary<AtomicCandidacyDecisionStatus,
            Func<Guid, CandidacyDecision, AtomicCandidacyDecisionResult, Result<DecideInternshipCandidacyResponse>>>
        {
            [AtomicCandidacyDecisionStatus.Decided] = (_, _, outcome) =>
                Result<DecideInternshipCandidacyResponse>.Success(
                    new DecideInternshipCandidacyResponse(outcome.Candidacy!.Id, outcome.Candidacy.Status)),
            [AtomicCandidacyDecisionStatus.CandidacyNotFound] = (id, _, _) =>
                Result<DecideInternshipCandidacyResponse>.NotFound(
                    $"Candidacy {id} not found."),
            [AtomicCandidacyDecisionStatus.OfferNotFound] = (id, _, _) =>
                Result<DecideInternshipCandidacyResponse>.NotFound(
                    $"Offer of candidacy {id} not found."),
            [AtomicCandidacyDecisionStatus.InvalidTransition] = (id, decision, _) =>
                Result<DecideInternshipCandidacyResponse>.Conflict(
                    $"Candidacy {id} cannot be {Verbs[decision]} from its current status."),
            [AtomicCandidacyDecisionStatus.ConcurrencyConflict] = (id, _, _) =>
                Result<DecideInternshipCandidacyResponse>.Conflict(
                    $"Candidacy {id} could not be decided because the offer was modified concurrently. Retry the request.")
        };

    public async Task<Result<DecideInternshipCandidacyResponse>> HandleAsync(
        DecideInternshipCandidacyCommand command,
        CancellationToken cancellationToken = default)
    {
        var decisionName = Enum.GetNames<CandidacyDecision>()
            .FirstOrDefault(name => string.Equals(name, command.Action, StringComparison.OrdinalIgnoreCase));

        if (decisionName is null)
            return Result<DecideInternshipCandidacyResponse>.Validation(
                $"Action '{command.Action}' is not valid. Allowed values: Shortlist, Accept, Reject.");

        var decision = Enum.Parse<CandidacyDecision>(decisionName);

        var outcome = await decisions.DecideAsync(command.CandidacyId, decision, cancellationToken);

        return Outcomes[outcome.Status](command.CandidacyId, decision, outcome);
    }
}
