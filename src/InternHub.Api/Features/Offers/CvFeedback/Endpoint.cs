using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CvFeedback;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost(
            "/offers/{offerId:guid}/cv-feedback",
            async (
                Guid offerId,
                CvFeedbackRequest request,
                ICvFeedbackHandler handler,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new CvFeedbackCommand(offerId, request.CvText),
                    cancellationToken);

                return result.Status == ResultStatus.Success
                    ? Results.Ok(result.Value)
                    : result.ToFailure();
            });
}
