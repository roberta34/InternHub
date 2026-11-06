using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/students/{studentId:guid}/candidacies/summary", (
            Guid studentId,
            IGetStudentCandidacySummaryHandler handler) =>
        {
            var result = handler.Handle(new GetStudentCandidacySummaryQuery(studentId));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
