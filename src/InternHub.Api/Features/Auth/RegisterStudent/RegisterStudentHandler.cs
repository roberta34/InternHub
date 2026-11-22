using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.RegisterStudent;

public sealed class RegisterStudentHandler(IStudentAccountRegistration registration)
    : IRegisterStudentHandler
{
    public async Task<Result<RegisterStudentResponse>> HandleAsync(
        RegisterStudentCommand command,
        CancellationToken ct)
    {
        var fullName = command.FullName;
        var email = command.Email;
        var password = command.Password;

        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return Result<RegisterStudentResponse>.Validation(
                "Full name, email, and password are required.");
        }

        Result<RegisteredAccount> registrationResult;

        try
        {
            registrationResult = await registration.RegisterAsync(fullName, email, password, ct);
        }
        catch (ArgumentException exception)
        {
            return Result<RegisterStudentResponse>.Validation(exception.Message);
        }

        return registrationResult.Status switch
        {
            ResultStatus.Success => Result<RegisterStudentResponse>.Success(
                new RegisterStudentResponse(
                    registrationResult.Value!.AccountId,
                    registrationResult.Value.StudentId,
                    registrationResult.Value.Email,
                    "Student")),
            ResultStatus.Validation => Result<RegisterStudentResponse>.Validation(
                GetError(registrationResult)),
            ResultStatus.NotFound => Result<RegisterStudentResponse>.NotFound(
                GetError(registrationResult)),
            ResultStatus.Conflict => Result<RegisterStudentResponse>.Conflict(
                GetError(registrationResult)),
            ResultStatus.Unauthorized => Result<RegisterStudentResponse>.Unauthorized(
                GetError(registrationResult)),
            ResultStatus.ServiceUnavailable => Result<RegisterStudentResponse>.ServiceUnavailable(
                GetError(registrationResult)),
            _ => throw new InvalidOperationException(
                $"Unexpected registration result status: {registrationResult.Status}.")
        };
    }

    private static string GetError(Result<RegisteredAccount> result) =>
        result.Error ?? throw new InvalidOperationException(
            $"A {result.Status} registration result must include an error.");
}
