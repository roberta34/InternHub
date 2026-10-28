using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.Students.UpdateStudent;

public sealed class UpdateStudentHandler(IStudentRepository students) : IUpdateStudentHandler
{
    public Result<bool> Handle(UpdateStudentCommand command)
    {
        var student = students.GetById(command.Id);

        if (student is null)
            return Result<bool>.NotFound($"Student {command.Id} not found.");

        try
        {
            student.Update(command.FullName, command.Email);
        }
        catch (ArgumentException ex)
        {
            return Result<bool>.Validation(ex.Message);
        }

        students.Update(student);

        return Result<bool>.Success(true);
    }
}
