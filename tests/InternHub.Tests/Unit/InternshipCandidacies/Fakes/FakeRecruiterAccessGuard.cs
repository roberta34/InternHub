using System.Security.Claims;
using InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;

namespace InternHub.Tests.Unit.InternshipCandidacies.Fakes;

internal sealed class FakeRecruiterAccessGuard(RecruiterAccessResult result) : IRecruiterAccessGuard
{
    public RecruiterAccessResult Result { get; set; } = result;
    public int CallCount { get; private set; }
    public ClaimsPrincipal? LastUser { get; private set; }

    public static FakeRecruiterAccessGuard AuthorizedAs(Guid recruiterId) =>
        new(RecruiterAccessResult.Authorized(recruiterId));

    public static FakeRecruiterAccessGuard Unauthorized() =>
        new(RecruiterAccessResult.Unauthorized("Recruiter authentication is required."));

    public static FakeRecruiterAccessGuard Forbidden() =>
        new(RecruiterAccessResult.Forbidden("Authenticated recruiter identity is missing or invalid."));

    public Task<RecruiterAccessResult> ResolveAuthenticatedRecruiterAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        LastUser = user;
        return Task.FromResult(Result);
    }
}
