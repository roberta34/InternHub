using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.DeleteOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapDelete("/offers/{id:guid}", (
            Guid id,
            IDeleteOfferHandler handler) =>
        {
            var result = handler.Handle(new DeleteOfferCommand(id));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
