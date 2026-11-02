using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies/{id:guid}/decision", (
            Guid id,
            DecideInternshipCandidacyRequest request,
            IDecideInternshipCandidacyHandler handler) =>
        {
            var result = handler.Handle(new DecideInternshipCandidacyCommand(id, request.Action));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
