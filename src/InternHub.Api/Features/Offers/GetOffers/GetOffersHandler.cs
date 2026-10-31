using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOffers;

public sealed class GetOffersHandler(
    IOfferRepository offers,
    TimeProvider timeProvider) : IGetOffersHandler
{
    public Result<IReadOnlyList<GetOffersResponse>> Handle(GetOffersQuery query)
    {
        IEnumerable<Offer> filteredOffers = offers.GetAll();

        if (query.ActiveOnly)
        {
            var now = timeProvider.GetUtcNow().UtcDateTime;

            filteredOffers = filteredOffers.Where(offer =>
                !offer.IsClosed &&
                offer.Deadline > now);
        }

        var response = filteredOffers
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