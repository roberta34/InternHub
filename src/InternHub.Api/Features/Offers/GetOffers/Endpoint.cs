using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Mvc;

namespace InternHub.Api.Features.Offers.GetOffers;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/offers", (
            [FromQuery] bool? activeOnly,
            IGetOffersHandler handler) =>
        {
            var query = new GetOffersQuery(activeOnly ?? false);
            var result = handler.Handle(query);

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}