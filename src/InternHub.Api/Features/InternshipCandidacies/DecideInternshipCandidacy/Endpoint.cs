using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies/{id:guid}/decision", async (
            Guid id,
            DecideInternshipCandidacyRequest request,
            IDecideInternshipCandidacyHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new DecideInternshipCandidacyCommand(id, request.Action),
                cancellationToken);

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
