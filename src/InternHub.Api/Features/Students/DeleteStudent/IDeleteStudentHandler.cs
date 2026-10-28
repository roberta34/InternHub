using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.DeleteStudent;

public interface IDeleteStudentHandler
{
    Result<bool> Handle(DeleteStudentCommand command);
}
