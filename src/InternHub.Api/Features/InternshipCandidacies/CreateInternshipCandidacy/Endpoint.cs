using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies", (
            CreateInternshipCandidacyRequest request,
            ICreateInternshipCandidacyHandler handler) =>
        {
            var result = handler.Handle(
                new CreateInternshipCandidacyCommand(request.StudentId, request.OfferId));

            return result.Status == ResultStatus.Success
                ? Results.Created($"/api/v1/candidacies/{result.Value!.Id}", result.Value)
                : result.ToFailure();
        });
}
