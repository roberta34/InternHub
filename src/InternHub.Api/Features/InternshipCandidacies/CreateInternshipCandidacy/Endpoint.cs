using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies", async (
            CreateInternshipCandidacyRequest request,
            ICreateInternshipCandidacyHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new CreateInternshipCandidacyCommand(request.StudentId, request.OfferId),
                cancellationToken);

            return result.Status == ResultStatus.Success
                ? Results.Created($"/api/v1/candidacies/{result.Value!.Id}", result.Value)
                : result.ToFailure();
        });
}
