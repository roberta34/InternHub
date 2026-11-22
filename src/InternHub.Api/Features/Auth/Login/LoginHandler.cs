using System.Security.Claims;
using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.Login;

public sealed class LoginHandler(IAccountAuthenticator authenticator) : ILoginHandler
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<Result<ClaimsPrincipal>> HandleAsync(LoginCommand command, CancellationToken ct)
    {
        var email = command.Email;
        var password = command.Password;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return Result<ClaimsPrincipal>.Validation("Email and password are required.");

        var result = await authenticator.AuthenticateAsync(email, password, ct);

        return result.Status == ResultStatus.Unauthorized
            ? Result<ClaimsPrincipal>.Unauthorized(InvalidCredentialsMessage)
            : result;
    }
}
