using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.Students.CreateStudent;

public sealed class CreateStudentHandler(IStudentRepository students) : ICreateStudentHandler
{
    public Result<CreateStudentResponse> Handle(CreateStudentCommand command)
    {
        Student student;

        try
        {
            student = new Student(Guid.NewGuid(), command.FullName, command.Email);
        }
        catch (ArgumentException exception)
        {
            return Result<CreateStudentResponse>.Validation(exception.Message);
        }

        if (!students.Add(student))
            return Result<CreateStudentResponse>.Conflict($"Student {student.Id} already exists.");

        return Result<CreateStudentResponse>.Success(
            new CreateStudentResponse(student.Id, student.FullName, student.Email));
    }
}
