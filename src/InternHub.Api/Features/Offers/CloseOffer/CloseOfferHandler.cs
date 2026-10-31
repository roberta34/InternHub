using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CloseOffer;

public sealed class CloseOfferHandler(IOfferRepository offers)
    : ICloseOfferHandler
{
    public Result<bool> Handle(CloseOfferCommand command)
    {
        var offer = offers.GetById(command.Id);

        if (offer is null)
            return Result<bool>.NotFound(
                $"Offer {command.Id} not found.");

        if (offer.IsClosed)
            return Result<bool>.Success(true);

        offer.Close();

        try
        {
            var updated = offers.Update(offer);

            return updated
                ? Result<bool>.Success(true)
                : Result<bool>.NotFound(
                    $"Offer {command.Id} not found.");
        }
        catch (OfferConcurrencyException)
        {
            return Result<bool>.Conflict(
                $"Offer {command.Id} was modified concurrently.");
        }
    }
}