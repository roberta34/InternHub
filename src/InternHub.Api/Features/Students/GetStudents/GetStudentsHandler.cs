using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Features.Students.GetStudents;

namespace InternHub.Api.Features.Students.GetStudents;

public sealed class GetStudentsHandler(IStudentRepository students) : IGetStudentsHandler
{
    public Result<IReadOnlyList<GetStudentsResponse>> Handle(GetStudentsQuery query)
    {
        var response = students.GetAll()
            .Select(student => new GetStudentsResponse(student.Id, student.FullName, student.Email))
            .ToList();

        return Result<IReadOnlyList<GetStudentsResponse>>.Success(response);
    }
}
