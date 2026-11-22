using System.Security.Claims;
using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Shared;

namespace InternHub.Tests.Unit.Auth.Fakes;

internal sealed class FakeAccountAuthenticator : IAccountAuthenticator
{
    public Result<ClaimsPrincipal> ResultToReturn { get; set; } =
        Result<ClaimsPrincipal>.Unauthorized("No authentication result configured.");

    public int CallCount { get; private set; }
    public string? LastEmail { get; private set; }
    public string? LastPassword { get; private set; }

    public Task<Result<ClaimsPrincipal>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken ct)
    {
        CallCount++;
        LastEmail = email;
        LastPassword = password;
        return Task.FromResult(ResultToReturn);
    }
}
