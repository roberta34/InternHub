using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.DeleteStudent;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapDelete("/students/{id:guid}", (
            Guid id,
            IDeleteStudentHandler handler) =>
        {
            var result = handler.Handle(new DeleteStudentCommand(id));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
