using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOfferById;

public sealed class GetOfferByIdHandler(IOfferRepository offers) : IGetOfferByIdHandler
{
    public Result<GetOfferByIdResponse> Handle(GetOfferByIdQuery query)
    {
        var offer = offers.GetById(query.Id);

        return offer is null
            ? Result<GetOfferByIdResponse>.NotFound($"Offer {query.Id} was not found.")
            : Result<GetOfferByIdResponse>.Success(
                new GetOfferByIdResponse(
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
