using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOffers;

public sealed class GetOffersHandler(IOfferRepository offers) : IGetOffersHandler
{
    public Result<IReadOnlyList<GetOffersResponse>> Handle(GetOffersQuery query)
    {
        var response = offers
            .GetAll()
            .Select(offer => new GetOffersResponse(
                offer.Id,
                offer.Title,
                offer.CompanyName,
                offer.Requirements,
                offer.Deadline,
                offer.MaximumAvailableSlots,
                offer.IsClosed,
                offer.Version))
            .ToList();

        return Result<IReadOnlyList<GetOffersResponse>>.Success(response);
    }
}
