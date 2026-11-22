using System.Security.Claims;
using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Identity;

namespace InternHub.Api.Infrastructure.Auth;

public sealed class IdentityAccountAuthenticator(
    UserManager<ApplicationUser> users,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsPrincipalFactory)
    : IAccountAuthenticator
{
    private const string InvalidCredentialsMessage = "Invalid email or password.";

    public async Task<Result<ClaimsPrincipal>> AuthenticateAsync(
        string email,
        string password,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var user = await users.FindByEmailAsync(email);
        if (user is null || !await users.CheckPasswordAsync(user, password))
            return Result<ClaimsPrincipal>.Unauthorized(InvalidCredentialsMessage);

        var principal = await claimsPrincipalFactory.CreateAsync(user);
        return Result<ClaimsPrincipal>.Success(principal);
    }
}
