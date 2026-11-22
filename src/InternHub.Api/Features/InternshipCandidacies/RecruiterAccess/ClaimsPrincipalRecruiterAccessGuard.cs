using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;

/// <summary>
/// Resolves the single authenticated recruiter from the claims written by InternHubClaimsFactory
/// ("role", "recruiter_id", "sub"). Anonymous callers get 401; anything ambiguous or not a
/// recruiter gets 403 (fail-closed).
/// </summary>
public sealed class ClaimsPrincipalRecruiterAccessGuard : IRecruiterAccessGuard
{
    private const string RecruiterRole = "Recruiter";
    private const string StudentRole = "Student";
    private const string RoleClaimType = "role";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string StudentIdClaimType = "student_id";
    private const string SubjectClaimType = "sub";

    public Task<RecruiterAccessResult> ResolveAuthenticatedRecruiterAsync(
        ClaimsPrincipal? user,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var authenticatedIdentities = user?.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();

        if (authenticatedIdentities is null || authenticatedIdentities.Length == 0)
        {
            return Task.FromResult(RecruiterAccessResult.Unauthorized(
                "Recruiter authentication is required."));
        }

        if (!TrySelectSingleRecruiterIdentity(authenticatedIdentities, out var recruiterIdentity))
        {
            return Task.FromResult(RecruiterAccessResult.Forbidden(
                "Only a single unambiguous authenticated recruiter identity can access candidacies."));
        }

        if (!TryExtractRecruiterId(recruiterIdentity, out var recruiterId))
        {
            return Task.FromResult(RecruiterAccessResult.Forbidden(
                "Authenticated recruiter identity is missing or invalid."));
        }

        return Task.FromResult(RecruiterAccessResult.Authorized(recruiterId));
    }

    private static bool TrySelectSingleRecruiterIdentity(
        ClaimsIdentity[] authenticatedIdentities,
        out ClaimsIdentity selectedIdentity)
    {
        selectedIdentity = null!;

        if (authenticatedIdentities.Any(HasStudentRole))
            return false;

        var eligibleIdentities = authenticatedIdentities
            .Where(HasValidRecruiterRole)
            .ToArray();

        if (eligibleIdentities.Length != 1)
            return false;

        var candidate = eligibleIdentities[0];

        var otherIdentitiesCarryIdentifiers = authenticatedIdentities
            .Where(identity => !ReferenceEquals(identity, candidate))
            .Any(identity => identity.Claims.Any(claim =>
                IsClaimType(claim, RecruiterIdClaimType) ||
                IsClaimType(claim, StudentIdClaimType) ||
                IsClaimType(claim, SubjectClaimType) ||
                IsClaimType(claim, ClaimTypes.NameIdentifier)));

        if (otherIdentitiesCarryIdentifiers)
            return false;

        selectedIdentity = candidate;
        return true;
    }

    private static bool HasStudentRole(ClaimsIdentity identity) =>
        GetRoleClaimValues(identity)
            .Any(role => string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase));

    private static bool HasValidRecruiterRole(ClaimsIdentity identity)
    {
        var roleValues = GetRoleClaimValues(identity);

        if (roleValues.Length == 0 || roleValues.Any(string.IsNullOrWhiteSpace))
            return false;

        return roleValues.Any(role => string.Equals(role, RecruiterRole, StringComparison.OrdinalIgnoreCase))
            && !roleValues.Any(role => string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase));
    }

    private static string[] GetRoleClaimValues(ClaimsIdentity identity) =>
        identity.Claims
            .Where(claim =>
                IsClaimType(claim, ClaimTypes.Role) ||
                IsClaimType(claim, identity.RoleClaimType) ||
                IsClaimType(claim, RoleClaimType))
            .Select(claim => claim.Value)
            .ToArray();

    private static bool TryExtractRecruiterId(ClaimsIdentity identity, out Guid recruiterId)
    {
        recruiterId = Guid.Empty;

        if (!TryValidateOptionalAccountIdentifiers(identity))
            return false;

        if (identity.Claims.Any(claim => IsClaimType(claim, StudentIdClaimType)))
            return false;

        var recruiterIdClaims = identity.Claims
            .Where(claim => IsClaimType(claim, RecruiterIdClaimType))
            .ToArray();

        return recruiterIdClaims.Length == 1
            && Guid.TryParse(recruiterIdClaims[0].Value, out recruiterId)
            && recruiterId != Guid.Empty;
    }

    private static bool TryValidateOptionalAccountIdentifiers(ClaimsIdentity identity)
    {
        var subjects = identity.Claims
            .Where(claim => IsClaimType(claim, SubjectClaimType))
            .Select(claim => claim.Value)
            .ToArray();
        var nameIdentifiers = identity.Claims
            .Where(claim => IsClaimType(claim, ClaimTypes.NameIdentifier))
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

    private static bool IsClaimType(Claim claim, string claimType) =>
        string.Equals(claim.Type, claimType, StringComparison.OrdinalIgnoreCase);
}
