using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Students.CreateStudent;

public interface ICreateStudentHandler
{
    Result<CreateStudentResponse> Handle(CreateStudentCommand command);
}
