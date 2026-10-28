using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CreateOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/offers", (CreateOfferRequest request, ICreateOfferHandler handler) =>
        {
            var result = handler.Handle(new CreateOfferCommand(
                request.Title,
                request.CompanyName,
                request.Requirements,
                request.Deadline,
                request.MaximumAvailableSlots));

            return result.Status == ResultStatus.Success
                ? Results.Created($"/api/v1/offers/{result.Value!.Id}", result.Value)
                : result.ToFailure();
        });
}
