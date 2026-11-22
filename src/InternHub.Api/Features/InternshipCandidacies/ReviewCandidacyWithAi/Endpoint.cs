using System.Security.Claims;
using InternHub.Api.Features.Shared;
using InternHub.Api.Infrastructure.AI.Fallback;

namespace InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies/{candidacyId:guid}/ai-review", async (
            Guid candidacyId,
            ReviewCandidacyWithAiRequest? request,
            ClaimsPrincipal user,
            IReviewCandidacyWithAiHandler handler,
            AiExecutionTrace executionTrace,
            HttpContext httpContext,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new ReviewCandidacyWithAiCommand(candidacyId, request?.CvText, user),
                cancellationToken);

            if (result.Status != ResultStatus.Success)
                return result.ToFailure();

            httpContext.Response.Headers["X-InternHub-AI-Provider"] = executionTrace.Provider;
            httpContext.Response.Headers["X-InternHub-AI-Fallback"] =
                executionTrace.FallbackUsed ? "true" : "false";

            return Results.Ok(result.Value);
        });
}
