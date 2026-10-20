using InternHub.Domain.Students;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Features.Students.UpdateStudent;

public static class UpdateStudentEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/students/{id:guid}", Handle);

    private static Results<NoContent, BadRequest<string>, NotFound> Handle(
        Guid id,
        UpdateStudentRequest request,
        IStudentRepository repository)
    {
        var student = repository.GetById(id);
        if (student is null)
            return TypedResults.NotFound();

        try
        {
            student.Update(request.FullName!, request.Email!);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        return repository.Update(student)
            ? TypedResults.NoContent()
            : TypedResults.NotFound();
    }
}