using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;

public interface IRecruiterAccessGuard
{
    Task<RecruiterAccessResult> ResolveAuthenticatedRecruiterAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default);
}
