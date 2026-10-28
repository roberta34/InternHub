using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOffers;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/offers", (IGetOffersHandler handler) =>
        {
            var result = handler.Handle(new GetOffersQuery());

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
