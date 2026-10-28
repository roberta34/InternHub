using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.GetStudents;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/students", (IGetStudentsHandler handler) =>
        {
            var result = handler.Handle(new GetStudentsQuery());

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
