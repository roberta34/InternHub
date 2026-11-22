using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Shared;

namespace InternHub.Tests.Unit.Auth.Fakes;

internal sealed class FakeStudentAccountRegistration : IStudentAccountRegistration
{
    public Result<RegisteredAccount> ResultToReturn { get; set; } =
        Result<RegisteredAccount>.Validation("No registration result configured.");

    public Exception? ExceptionToThrow { get; set; }
    public int CallCount { get; private set; }
    public string? LastFullName { get; private set; }
    public string? LastEmail { get; private set; }
    public string? LastPassword { get; private set; }

    public Task<Result<RegisteredAccount>> RegisterAsync(
        string fullName,
        string email,
        string password,
        CancellationToken ct)
    {
        CallCount++;
        LastFullName = fullName;
        LastEmail = email;
        LastPassword = password;

        var exception = ExceptionToThrow;
        return exception is null
            ? Task.FromResult(ResultToReturn)
            : Task.FromException<Result<RegisteredAccount>>(exception);
    }
}
