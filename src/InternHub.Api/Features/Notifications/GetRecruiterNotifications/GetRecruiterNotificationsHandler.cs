using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Notifications.GetRecruiterNotifications;

public sealed class GetRecruiterNotificationsHandler(
    IRecruiterCompanyAccessLookup companyAccessLookup,
    INotificationOutboxReader notifications) : IGetRecruiterNotificationsHandler
{
    private const string RecruiterRole = "Recruiter";
    private const string StudentRole = "Student";
    private const string RoleClaimType = "role";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    public async Task<Result<IReadOnlyList<GetRecruiterNotificationsResponse>>> HandleAsync(
        GetRecruiterNotificationsQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var authenticatedIdentities = query.User?.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();

        if (authenticatedIdentities is null || authenticatedIdentities.Length == 0)
        {
            return Result<IReadOnlyList<GetRecruiterNotificationsResponse>>.Unauthorized(
                "Authentication is required to view recruiter notifications.");
        }

        if (!TrySelectSingleRecruiterIdentity(authenticatedIdentities, out var recruiterIdentity))
        {
            return Result<IReadOnlyList<GetRecruiterNotificationsResponse>>.Forbidden(
                "Only a single unambiguous authenticated recruiter identity can view recruiter notifications.");
        }

        if (!TryExtractRecruiterId(recruiterIdentity, out var recruiterId))
        {
            return Result<IReadOnlyList<GetRecruiterNotificationsResponse>>.Forbidden(
                "Authenticated recruiter identity is missing or invalid.");
        }

        var companyId = await companyAccessLookup.GetRecruiterCompanyIdAsync(
            recruiterId,
            cancellationToken);

        if (companyId is null || companyId == Guid.Empty)
        {
            return Result<IReadOnlyList<GetRecruiterNotificationsResponse>>.Forbidden(
                "Authenticated recruiter is not associated with a valid company.");
        }

        var companyOfferIds = await companyAccessLookup.GetCompanyOfferIdsAsync(
            companyId.Value,
            cancellationToken);

        var allowedOfferIds = companyOfferIds
            .Where(offerId => offerId != Guid.Empty)
            .Distinct()
            .ToArray();

        if (allowedOfferIds.Length == 0)
        {
            return Result<IReadOnlyList<GetRecruiterNotificationsResponse>>.Success([]);
        }

        var allowedOfferIdSet = allowedOfferIds.ToHashSet();
        var entries = await notifications.GetByOfferIdsAsync(
            allowedOfferIds,
            cancellationToken);

        var response = entries
            .Where(entry => allowedOfferIdSet.Contains(entry.OfferId))
            .OrderByDescending(entry => entry.OccurredAtUtc)
            .ThenBy(entry => entry.Id)
            .Select(entry => new GetRecruiterNotificationsResponse(
                entry.Id,
                entry.CandidacyId,
                entry.OfferId,
                entry.StudentId,
                entry.Kind,
                entry.OccurredAtUtc))
            .ToList();

        return Result<IReadOnlyList<GetRecruiterNotificationsResponse>>.Success(response);
    }

    private static bool TrySelectSingleRecruiterIdentity(
        ClaimsIdentity[] authenticatedIdentities,
        out ClaimsIdentity selectedIdentity)
    {
        selectedIdentity = null!;

        if (authenticatedIdentities.Any(HasStudentRole))
        {
            return false;
        }

        var eligibleIdentities = authenticatedIdentities
            .Where(HasValidRecruiterRole)
            .ToArray();

        if (eligibleIdentities.Length != 1)
        {
            return false;
        }

        var candidate = eligibleIdentities[0];

        var otherIdentitiesCarryIdentifiers = authenticatedIdentities
            .Where(identity => !ReferenceEquals(identity, candidate))
            .Any(identity => identity.Claims.Any(claim =>
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

    private static bool HasStudentRole(ClaimsIdentity identity) =>
        GetRoleClaimValues(identity)
            .Any(role => string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase));

    private static bool HasValidRecruiterRole(ClaimsIdentity identity)
    {
        var roleValues = GetRoleClaimValues(identity);

        if (roleValues.Length == 0 || roleValues.Any(string.IsNullOrWhiteSpace))
        {
            return false;
        }

        var hasRecruiterRole = roleValues.Any(role =>
            string.Equals(role, RecruiterRole, StringComparison.OrdinalIgnoreCase));
        var hasStudentRole = roleValues.Any(role =>
            string.Equals(role, StudentRole, StringComparison.OrdinalIgnoreCase));

        return hasRecruiterRole && !hasStudentRole;
    }

    private static string[] GetRoleClaimValues(ClaimsIdentity identity) =>
        identity.Claims
            .Where(claim =>
                string.Equals(claim.Type, ClaimTypes.Role, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, identity.RoleClaimType, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(claim.Type, RoleClaimType, StringComparison.OrdinalIgnoreCase))
            .Select(claim => claim.Value)
            .ToArray();

    private static bool TryExtractRecruiterId(
        ClaimsIdentity identity,
        out Guid recruiterId)
    {
        recruiterId = Guid.Empty;

        if (!TryValidateOptionalAccountIdentifiers(identity))
        {
            return false;
        }

        var recruiterIdClaims = identity.Claims
            .Where(claim => string.Equals(claim.Type, RecruiterIdClaimType, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (recruiterIdClaims.Length != 1)
        {
            return false;
        }

        return TryParseNonEmptyGuid(recruiterIdClaims[0].Value, out recruiterId);
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
