using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.RegisterStudent;

public interface IRegisterStudentHandler
{
    Task<Result<RegisterStudentResponse>> HandleAsync(
        RegisterStudentCommand command,
        CancellationToken ct);
}
