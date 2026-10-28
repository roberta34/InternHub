using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.UpdateOffer;

public sealed class UpdateOfferHandler(IOfferRepository offers) : IUpdateOfferHandler
{
    public Result<bool> Handle(UpdateOfferCommand command)
    {
        var offer = offers.GetById(command.Id);

        if (offer is null)
            return Result<bool>.NotFound($"Offer {command.Id} not found.");

        if (offer.Version != command.Version)
            return Result<bool>.Conflict($"Offer {command.Id} has a stale version.");

        try
        {
            offer.Update(
                command.Title,
                command.CompanyName,
                command.Requirements,
                command.Deadline,
                command.MaximumAvailableSlots);
        }
        catch (ArgumentException exception)
        {
            return Result<bool>.Validation(exception.Message);
        }

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
