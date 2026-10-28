using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.UpdateStudent;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPut("/students/{id:guid}", (
            Guid id,
            UpdateStudentRequest request,
            IUpdateStudentHandler handler) =>
        {
            var result = handler.Handle(new UpdateStudentCommand(id, request.FullName, request.Email));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
