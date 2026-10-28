using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.UpdateStudent;

public interface IUpdateStudentHandler
{
    Result<bool> Handle(UpdateStudentCommand command);
}
