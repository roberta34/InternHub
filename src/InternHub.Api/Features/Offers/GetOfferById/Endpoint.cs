using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOfferById;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/offers/{id:guid}", (Guid id, IGetOfferByIdHandler handler) =>
        {
            var result = handler.Handle(new GetOfferByIdQuery(id));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
