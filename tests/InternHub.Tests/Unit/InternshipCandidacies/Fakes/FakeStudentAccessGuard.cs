using System.Security.Claims;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;

namespace InternHub.Tests.Unit.InternshipCandidacies.Fakes;

internal sealed class FakeStudentAccessGuard : IStudentAccessGuard
{
    public Func<ClaimsPrincipal?, CancellationToken, StudentAccessResult>? ResolveHandler { get; set; }
    public Func<ClaimsPrincipal?, Guid, CancellationToken, StudentAccessResult>? AuthorizeHandler { get; set; }

    public int ResolveCallCount { get; private set; }
    public int AuthorizeCallCount { get; private set; }
    public ClaimsPrincipal? LastUser { get; private set; }
    public Guid? LastRequestedStudentId { get; private set; }
    public CancellationToken LastCancellationToken { get; private set; }

    public Task<StudentAccessResult> ResolveAuthenticatedStudentAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ResolveCallCount++;
        LastUser = user;
        LastCancellationToken = cancellationToken;

        var result = ResolveHandler?.Invoke(user, cancellationToken)
            ?? StudentAccessResult.Forbidden("Unconfigured fake guard.");
        return Task.FromResult(result);
    }

    public Task<StudentAccessResult> AuthorizeStudentAccessAsync(
        ClaimsPrincipal? user,
        Guid requestedStudentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        AuthorizeCallCount++;
        LastUser = user;
        LastRequestedStudentId = requestedStudentId;
        LastCancellationToken = cancellationToken;

        var result = AuthorizeHandler?.Invoke(user, requestedStudentId, cancellationToken)
            ?? StudentAccessResult.Authorized(requestedStudentId);
        return Task.FromResult(result);
    }
}
