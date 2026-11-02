using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/offers/{offerId:guid}/candidacies", (
            Guid offerId,
            IGetOfferCandidaciesHandler handler) =>
        {
            var result = handler.Handle(new GetOfferCandidaciesQuery(offerId));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
