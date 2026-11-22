using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.StudentAccess;

public sealed class ClaimsPrincipalStudentAccessGuard : IStudentAccessGuard
{
    private const string StudentRole = "Student";
    private const string RecruiterRole = "Recruiter";
    private const string RoleClaimType = "role";
    private const string StudentIdClaimType = "student_id";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    public Task<StudentAccessResult> ResolveAuthenticatedStudentAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var authenticatedIdentities = user?.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();

        if (authenticatedIdentities is null || authenticatedIdentities.Length == 0)
        {
            return Task.FromResult(StudentAccessResult.Unauthorized(
                "Authentication is required to access student candidacies."));
        }

        if (!TrySelectSingleStudentIdentity(authenticatedIdentities, out var studentIdentity))
        {
            return Task.FromResult(StudentAccessResult.Forbidden(
                "Only a single unambiguous authenticated student identity can access student candidacies."));
        }

        if (!TryExtractStudentId(studentIdentity, out var studentId))
        {
            return Task.FromResult(StudentAccessResult.Forbidden(
                "Authenticated student identity is missing or invalid."));
        }

        return Task.FromResult(StudentAccessResult.Authorized(studentId));
    }

    public async Task<StudentAccessResult> AuthorizeStudentAccessAsync(
        ClaimsPrincipal? user,
        Guid requestedStudentId,
        CancellationToken cancellationToken = default)
    {
        var resolved = await ResolveAuthenticatedStudentAsync(user, cancellationToken);

        if (resolved.Status != StudentAccessStatus.Authorized)
        {
            return resolved;
        }

        if (requestedStudentId == Guid.Empty || resolved.StudentId!.Value != requestedStudentId)
        {
            return StudentAccessResult.Forbidden(
                "Authenticated student cannot access another student's candidacies.");
        }

        return resolved;
    }

    private static bool TrySelectSingleStudentIdentity(
        ClaimsIdentity[] authenticatedIdentities,
        out ClaimsIdentity selectedIdentity)
    {
        selectedIdentity = null!;

        if (authenticatedIdentities.Any(HasRecruiterRole))
        {
            return false;
        }

        var eligibleIdentities = authenticatedIdentities
            .Where(HasValidStudentRole)
            .ToArray();

        if (eligibleIdentities.Length != 1)
        {
            return false;
        }

        var candidate = eligibleIdentities[0];

        var otherIdentitiesCarryIdentifiers = authenticatedIdentities
            .Where(identity => !ReferenceEquals(identity, candidate))
            .Any(identity => identity.Claims.Any(claim =>
                string.Equals(claim.Type, StudentIdClaimType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, RecruiterIdClaimType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, SubjectClaimType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase)));

        if (otherIdentitiesCarryIdentifiers)
        {
            return false;
        }

        selectedIdentity = candidate;
        return true;
    }

    private static bool HasRecruiterRole(ClaimsIdentity identity) =>
        GetRoleClaimValues(identity)
            .Any(role => string.Equals(role, RecruiterRole, StringComparison.OrdinalIgnoreCase));

    private static bool HasValidStudentRole(ClaimsIdentity identity)
    {
        var roleValues = GetRoleClaimValues(identity);

        if (roleValues.Length == 0 || roleValues.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        var hasStudentRole = roleValues.Any(role =>
            string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase));
        var hasRecruiterRole = roleValues.Any(role =>
            string.Equals(role, RecruiterRole, StringComparison.OrdinalIgnoreCase));

        return hasStudentRole && !hasRecruiterRole;
    }

    private static string[] GetRoleClaimValues(ClaimsIdentity identity) =>
        identity.Claims
            .Where(claim =>
                string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, identity.RoleClaimType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, RoleClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .ToArray();

    private static bool TryExtractStudentId(
        ClaimsIdentity identity,
        out Guid studentId)
    {
        studentId = Guid.Empty;

        if (!TryValidateOptionalAccountIdentifiers(identity))
        {
            return false;
        }

        var hasContradictoryRecruiterId = identity.Claims.Any(claim =>
            string.Equals(claim.Type, RecruiterIdClaimType, StringComparison.OrdinalIgnoreCase));

        if (hasContradictoryRecruiterId)
        {
            return false;
        }

        var studentIdClaims = identity.Claims
            .Where(claim => string.Equals(claim.Type, StudentIdClaimType, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (studentIdClaims.Length != 1)
        {
            return false;
        }

        return TryParseNonEmptyGuid(studentIdClaims[0].Value, out studentId);
    }

    private static bool TryValidateOptionalAccountIdentifiers(ClaimsIdentity identity)
    {
        var subjects = identity.Claims
            .Where(claim => string.Equals(claim.Type, SubjectClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .ToArray();
        var nameIdentifiers = identity.Claims
            .Where(claim => string.Equals(claim.Type, ClaimTypes.NameIdentifier, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .ToArray();

        if (subjects.Length > 1 || nameIdentifiers.Length > 1 ||
            subjects.Any(string.IsNullOrWhiteSpace) ||
            nameIdentifiers.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        return subjects.Length == 0 || nameIdentifiers.Length == 0 ||
               string.Equals(subjects[0], nameIdentifiers[0], StringComparison.Ordinal);
    }

    private static bool TryParseNonEmptyGuid(string? value, out Guid parsed)
    {
        parsed = Guid.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return Guid.TryParse(value, out parsed) && parsed != Guid.Empty;
    }
}
