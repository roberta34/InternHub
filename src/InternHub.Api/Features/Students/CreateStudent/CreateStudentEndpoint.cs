using InternHub.Domain.Students;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Features.Students.CreateStudent;

public sealed record CreateStudentRequest(string? FullName, string? Email);
public sealed record CreateStudentResponse(Guid Id, string FullName, string Email);

public static class CreateStudentEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPost("/students", Handle);

    private static Results<Created<CreateStudentResponse>, BadRequest<string>, Conflict<string>> Handle(
        CreateStudentRequest request, IStudentRepository repository)
    {
        Student student;
        try
        {
            student = new Student(Guid.NewGuid(), request.FullName!, request.Email!);
        }
        catch (ArgumentException ex)
        {
            return TypedResults.BadRequest(ex.Message);
        }

        if (!repository.Add(student))
            return TypedResults.Conflict($"Student {student.Id} already exists.");

        var response = new CreateStudentResponse(student.Id, student.FullName, student.Email);
        return TypedResults.Created($"/students/{student.Id}", response);
    }
}