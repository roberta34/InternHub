using InternHub.Api.Domain.Offers;

namespace InternHub.Api.Features.Offers.CreateOffer;

public static class CreateOfferEndpoint
{
    public static void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/offers", Handle);
    }

    private static IResult Handle(
        CreateOfferRequest request,
        IOfferRepository repository)
    {
        try
        {
            var offer = new Offer(
                request.Title,
                request.CompanyName,
                request.Requirements,
                request.Deadline,
                request.MaximumAvailableSlots);

            if (!repository.Add(offer))
            {
                return TypedResults.BadRequest("Offer could not be created.");
            }

            var response = new CreateOfferResponse(
                offer.Id,
                offer.Title,
                offer.CompanyName,
                offer.Requirements,
                offer.Deadline,
                offer.MaximumAvailableSlots,
                offer.IsClosed,
                offer.Version);

            return TypedResults.Created(
                $"/offers/{offer.Id}",
                response);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(exception.Message);
        }
    }
}

public record CreateOfferRequest(
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots);

public record CreateOfferResponse(
    Guid Id,
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    bool IsClosed,
    int Version);