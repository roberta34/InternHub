using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.Students.DeleteStudent;

public sealed class DeleteStudentHandler(IStudentRepository students) : IDeleteStudentHandler
{
    public Result<bool> Handle(DeleteStudentCommand command)
    {
        var deleted = students.Delete(command.Id);

        return deleted
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound($"Student {command.Id} not found.");
    }
}
