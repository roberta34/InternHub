using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.StudentAccess;

public interface IStudentAccessGuard
{
    Task<StudentAccessResult> ResolveAuthenticatedStudentAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default);

    Task<StudentAccessResult> AuthorizeStudentAccessAsync(
        ClaimsPrincipal? user,
        Guid requestedStudentId,
        CancellationToken cancellationToken = default);
}
