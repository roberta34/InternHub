using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CloseOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/offers/{id:guid}/close", (
            Guid id,
            ICloseOfferHandler handler) =>
        {
            var result = handler.Handle(new CloseOfferCommand(id));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}