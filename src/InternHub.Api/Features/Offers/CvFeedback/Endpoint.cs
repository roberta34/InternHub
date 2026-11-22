using InternHub.Api.Features.Shared;
using InternHub.Api.Infrastructure.AI.Fallback;

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
                AiExecutionTrace executionTrace,
                HttpContext httpContext,
                CancellationToken cancellationToken) =>
            {
                var result = await handler.HandleAsync(
                    new CvFeedbackCommand(offerId, request.CvText),
                    cancellationToken);

                if (result.Status != ResultStatus.Success)
                    return result.ToFailure();

                httpContext.Response.Headers["X-InternHub-AI-Provider"] =
                    executionTrace.Provider;
                httpContext.Response.Headers["X-InternHub-AI-Fallback"] =
                    executionTrace.FallbackUsed ? "true" : "false";

                return Results.Ok(result.Value);
            });
}
