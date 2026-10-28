using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.GetStudentById;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapGet("/students/{id:guid}", (Guid id, IGetStudentByIdHandler handler) =>
        {
            var result = handler.Handle(new GetStudentByIdQuery(id));

            return result.Status == ResultStatus.Success
                ? Results.Ok(result.Value)
                : result.ToFailure();
        });
}
