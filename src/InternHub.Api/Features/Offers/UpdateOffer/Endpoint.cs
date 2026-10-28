using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.UpdateOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPut("/offers/{id:guid}", (
            Guid id,
            UpdateOfferRequest request,
            IUpdateOfferHandler handler) =>
        {
            var result = handler.Handle(new UpdateOfferCommand(
                id,
                request.Title,
                request.CompanyName,
                request.Requirements,
                request.Deadline,
                request.MaximumAvailableSlots,
                request.Version));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
