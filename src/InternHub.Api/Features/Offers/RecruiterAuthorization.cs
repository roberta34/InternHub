using System.Security.Claims;

namespace InternHub.Api.Features.Offers;

public static class RecruiterAuthorization
{
    public static bool TryGetRecruiterId(HttpContext httpContext, out Guid recruiterId)
    {
        recruiterId = Guid.Empty;

        if (httpContext.User.Identity?.IsAuthenticated != true)
            return false;

        var isRecruiter = httpContext.User.Claims.Any(claim =>
            (claim.Type == ClaimTypes.Role || claim.Type == "role")
            && string.Equals(claim.Value, "Recruiter", StringComparison.OrdinalIgnoreCase));

        if (!isRecruiter)
            return false;

        var claim = httpContext.User.FindFirst("recruiter_id");
        return claim is not null
            && Guid.TryParse(claim.Value, out recruiterId)
            && recruiterId != Guid.Empty;
    }
}
