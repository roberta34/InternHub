using InternHub.Api.Features.Shared;

namespace InternHub.Api.Domain.Auth;

public interface IStudentAccountRegistration
{
    Task<Result<RegisteredAccount>> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken ct);
}
