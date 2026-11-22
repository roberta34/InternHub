using InternHub.Api.Domain.Offers;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.UpdateOffer;

public sealed class UpdateOfferHandler(
    IOfferRepository offers,
    IInternshipCandidacyRepository? candidacies = null) : IUpdateOfferHandler
{
    public Result<bool> Handle(UpdateOfferCommand command)
    {
        if (!command.RecruiterId.HasValue || command.RecruiterId == Guid.Empty)
            return Result<bool>.Unauthorized("Recruiter authentication is required.");

        var offer = offers.GetById(command.Id);

        if (offer is null)
            return Result<bool>.NotFound($"Offer {command.Id} not found.");

        if (!offer.RecruiterId.HasValue || offer.RecruiterId != command.RecruiterId)
            return Result<bool>.Forbidden($"Only the owning Recruiter can update offer {command.Id}.");

        if (offer.Version != command.Version)
            return Result<bool>.Conflict($"Offer {command.Id} has a stale version.");

        var acceptedCount = candidacies?.CountAcceptedForOffer(offer.Id) ?? 0;
        var updateResult = offer.Update(
            command.Title,
            command.CompanyName,
            command.Requirements,
            command.Deadline,
            command.MaximumAvailableSlots,
            acceptedCount);

        if (updateResult.Status == ResultStatus.Validation)
            return Result<bool>.Validation(updateResult.Error!);

        if (updateResult.Status == ResultStatus.Conflict)
            return Result<bool>.Conflict(updateResult.Error!);

        try
        {
            if (!offers.Update(offer))
                return Result<bool>.NotFound($"Offer {command.Id} not found.");
        }
        catch (OfferConcurrencyException)
        {
            return Result<bool>.Conflict($"Offer {command.Id} was modified concurrently.");
        }

        return Result<bool>.Success(true);
    }
}
