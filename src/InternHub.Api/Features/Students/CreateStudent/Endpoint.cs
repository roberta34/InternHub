using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.CreateStudent;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/students", (CreateStudentRequest request, ICreateStudentHandler handler) =>
        {
            var result = handler.Handle(
                new CreateStudentCommand(request.FullName, request.Email));

            return result.Status == ResultStatus.Success
                ? Results.Created($"/api/v1/students/{result.Value!.Id}", result.Value)
                : result.ToFailure();
        });
}
