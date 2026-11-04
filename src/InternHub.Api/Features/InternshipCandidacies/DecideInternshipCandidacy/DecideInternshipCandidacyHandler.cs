using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public sealed class DecideInternshipCandidacyHandler(
    IInternshipCandidacyRepository candidacies,
    IOfferRepository offers) : IDecideInternshipCandidacyHandler
{
    public Result<DecideInternshipCandidacyResponse> Handle(DecideInternshipCandidacyCommand command)
    {
        var decisionName = Enum.GetNames<CandidacyDecision>()
            .FirstOrDefault(name => string.Equals(name, command.Action, StringComparison.OrdinalIgnoreCase));

        if (decisionName is null)
            return Result<DecideInternshipCandidacyResponse>.Validation(
                $"Action '{command.Action}' is not valid. Allowed values: Shortlist, Accept, Reject.");

        var decision = Enum.Parse<CandidacyDecision>(decisionName);

        var candidacy = candidacies.GetById(command.CandidacyId);

        if (candidacy is null)
            return Result<DecideInternshipCandidacyResponse>.NotFound(
                $"Candidacy {command.CandidacyId} not found.");

        var hasAvailableSlot = false;

        if (decision == CandidacyDecision.Accept)
        {
            var offer = offers.GetById(candidacy.OfferId);

            if (offer is null)
                return Result<DecideInternshipCandidacyResponse>.NotFound(
                    $"Offer {candidacy.OfferId} not found.");

            hasAvailableSlot = candidacies.CountAcceptedForOffer(offer.Id) < offer.MaximumAvailableSlots;
        }

        var (changed, verb) = decision switch
        {
            CandidacyDecision.Shortlist => (candidacy.Shortlist(), "shortlisted"),
            CandidacyDecision.Accept => (candidacy.Accept(hasAvailableSlot), "accepted"),
            _ => (candidacy.Reject(), "rejected")
        };

        if (!changed)
            return Result<DecideInternshipCandidacyResponse>.Conflict(
                $"Candidacy {command.CandidacyId} cannot be {verb} from status {candidacy.Status}.");

        if (!candidacies.Update(candidacy))
            return Result<DecideInternshipCandidacyResponse>.NotFound(
                $"Candidacy {command.CandidacyId} not found.");

        return Result<DecideInternshipCandidacyResponse>.Success(
            new DecideInternshipCandidacyResponse(candidacy.Id, candidacy.Status));
    }
}
