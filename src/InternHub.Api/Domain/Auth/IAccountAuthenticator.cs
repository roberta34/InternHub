using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Domain.Auth;

public interface IAccountAuthenticator
{
    Task<Result<ClaimsPrincipal>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken ct);
}
