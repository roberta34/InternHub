using InternHub.Api.Domain.Offers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Api.Features.Offers.UpdateOffer;

public static class UpdateOfferEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/offers/{id:guid}", Handle);

    private static Results<
        NoContent,
        NotFound,
        BadRequest<string>> Handle(
        Guid id,
        UpdateOfferRequest request,
        IOfferRepository repository)
    {
        var offer = repository.GetById(id);

        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        try
        {
            offer.Update(
                request.Title,
                request.CompanyName,
                request.Requirements,
                request.Deadline,
                request.MaximumAvailableSlots);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        if (!repository.Update(offer))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}