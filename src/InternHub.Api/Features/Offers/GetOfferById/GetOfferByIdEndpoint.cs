using InternHub.Api.Domain.Offers;

namespace InternHub.Api.Features.Offers.GetOfferById;

public static class GetOfferByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/offers/{id:guid}", Handle);
    }

    private static IResult Handle(
        Guid id,
        IOfferRepository repository)
    {
        var offer = repository.GetById(id);

        if (offer is null)
        {
            return TypedResults.NotFound();
        }

        var response = new GetOfferByIdResponse(
            offer.Id,
            offer.Title,
            offer.CompanyName,
            offer.Requirements,
            offer.Deadline,
            offer.MaximumAvailableSlots,
            offer.IsClosed,
            offer.Version);

        return TypedResults.Ok(response);
    }
}

public record GetOfferByIdResponse(
    Guid Id,
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    bool IsClosed,
    int Version);