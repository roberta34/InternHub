using InternHub.Api.Domain.Offers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Api.Features.Offers.GetOffers;

public static class GetOffersEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/offers", Handle);

    private static Ok<List<GetOffersResponse>> Handle(
        IOfferRepository repository)
    {
        var offers = repository
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

        return TypedResults.Ok(offers);
    }
}