using InternHub.Domain.Students;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Features.Students.GetStudentById;

public static class GetStudentByIdEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/students/{id:guid}", Handle);

    private static Results<Ok<GetStudentByIdResponse>, NotFound> Handle(Guid id, IStudentRepository repository)
    {
        var student = repository.GetById(id);
        return student is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new GetStudentByIdResponse(student.Id, student.FullName, student.Email));
    }
}