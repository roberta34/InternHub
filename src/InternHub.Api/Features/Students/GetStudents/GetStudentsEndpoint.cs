using InternHub.Domain.Students;
using Microsoft.AspNetCore.Http.HttpResults;

namespace InternHub.Features.Students.GetStudents;

public static class GetStudentsEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/students", Handle);

    private static Ok<IReadOnlyList<GetStudentsResponse>> Handle(IStudentRepository repository)
    {
        var students = repository.GetAll()
            .Select(student => new GetStudentsResponse(student.Id, student.FullName, student.Email))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<GetStudentsResponse>>(students);
    }
}