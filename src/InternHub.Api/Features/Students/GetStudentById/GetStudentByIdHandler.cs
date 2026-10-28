using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Features.Students.GetStudentById;

namespace InternHub.Api.Features.Students.GetStudentById;

public sealed class GetStudentByIdHandler(IStudentRepository students) : IGetStudentByIdHandler
{
    public Result<GetStudentByIdResponse> Handle(GetStudentByIdQuery query)
    {
        var student = students.GetById(query.Id);

        return student is null
            ? Result<GetStudentByIdResponse>.NotFound($"Student {query.Id} was not found.")
            : Result<GetStudentByIdResponse>.Success(
                new GetStudentByIdResponse(student.Id, student.FullName, student.Email));
    }
}
