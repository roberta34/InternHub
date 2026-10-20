using InternHub.Api.Domain.Offers;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Api.Features.Offers.DeleteOffer;

public static class DeleteOfferEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/offers/{id:guid}", Handle);

    private static Results<
        NoContent,
        NotFound> Handle(
        Guid id,
        IOfferRepository repository)
    {
        if (!repository.Delete(id))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.NoContent();
    }
}