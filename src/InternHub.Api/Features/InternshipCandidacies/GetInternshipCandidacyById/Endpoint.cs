using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/candidacies/{id:guid}", (Guid id, IGetInternshipCandidacyByIdHandler handler) =>
        {
            var result = handler.Handle(new GetInternshipCandidacyByIdQuery(id));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
