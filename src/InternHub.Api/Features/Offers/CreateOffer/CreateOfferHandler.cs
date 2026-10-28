using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CreateOffer;

public sealed class CreateOfferHandler(IOfferRepository offers) : ICreateOfferHandler
{
    public Result<CreateOfferResponse> Handle(CreateOfferCommand command)
    {
        Offer offer;

        try
        {
            offer = new Offer(
                command.Title,
                command.CompanyName,
                command.Requirements,
                command.Deadline,
                command.MaximumAvailableSlots);
        }
        catch (ArgumentException exception)
        {
            return Result<CreateOfferResponse>.Validation(exception.Message);
        }

        if (!offers.Add(offer))
            return Result<CreateOfferResponse>.Conflict($"Offer {offer.Id} already exists.");

        return Result<CreateOfferResponse>.Success(
            new CreateOfferResponse(
                offer.Id,
                offer.Title,
                offer.CompanyName,
                offer.Requirements,
                offer.Deadline,
                offer.MaximumAvailableSlots,
                offer.IsClosed,
                offer.Version));
    }
}
