using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/students/{studentId:guid}/candidacies", (
            Guid studentId,
            IGetStudentCandidaciesHandler handler) =>
        {
            var result = handler.Handle(new GetStudentCandidaciesQuery(studentId));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
