using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/students/{studentId:guid}/candidacies", async (
            Guid studentId,
            ClaimsPrincipal user,
            IGetStudentCandidaciesHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new GetStudentCandidaciesQuery(studentId, user),
                cancellationToken);

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
