using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/students/{studentId:guid}/candidacies/summary", async (
            Guid studentId,
            ClaimsPrincipal user,
            IGetStudentCandidacySummaryHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new GetStudentCandidacySummaryQuery(studentId, user),
                cancellationToken);

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
