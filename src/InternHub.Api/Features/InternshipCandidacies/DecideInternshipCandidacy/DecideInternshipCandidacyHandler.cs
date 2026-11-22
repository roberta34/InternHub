using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public sealed class DecideInternshipCandidacyHandler(
    IAtomicCandidacyDecision decisions,
    IRecruiterAccessGuard recruiterAccess,
    IInternshipCandidacyRepository candidacies,
    IOfferRepository offers) : IDecideInternshipCandidacyHandler
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
        var access = await recruiterAccess.ResolveAuthenticatedRecruiterAsync(command.User, cancellationToken);
        if (!access.IsAuthorized)
            return access.ToFailure<DecideInternshipCandidacyResponse>();

        var decisionName = Enum.GetNames<CandidacyDecision>()
            .FirstOrDefault(name => string.Equals(name, command.Action, StringComparison.OrdinalIgnoreCase));

        if (decisionName is null)
            return Result<DecideInternshipCandidacyResponse>.Validation(
                $"Action '{command.Action}' is not valid. Allowed values: Shortlist, Accept, Reject.");

        var decision = Enum.Parse<CandidacyDecision>(decisionName);

        // Read-only ownership pre-check: Offer.RecruiterId never changes, so it stays valid for the atomic decision.
        var candidacy = candidacies.GetById(command.CandidacyId);
        if (candidacy is null)
            return Result<DecideInternshipCandidacyResponse>.NotFound(
                $"Candidacy {command.CandidacyId} not found.");

        var offer = offers.GetById(candidacy.OfferId);
        if (offer is null)
            return Result<DecideInternshipCandidacyResponse>.NotFound(
                $"Offer of candidacy {command.CandidacyId} not found.");

        if (offer.RecruiterId != access.RecruiterId)
            return Result<DecideInternshipCandidacyResponse>.Forbidden(
                $"Only the owning Recruiter can decide candidacy {command.CandidacyId}.");

        var outcome = await decisions.DecideAsync(command.CandidacyId, decision, cancellationToken);

        return Outcomes[outcome.Status](command.CandidacyId, decision, outcome);
    }
}
