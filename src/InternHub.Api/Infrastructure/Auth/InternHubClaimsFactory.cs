using System.Security.Claims;
using InternHub.Api.Domain.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace InternHub.Api.Infrastructure.Auth;

public sealed class InternHubClaimsFactory(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IOptions<IdentityOptions> options)
    : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>(
        userManager,
        roleManager,
        options)
{
    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("sub", user.Id.ToString("D")));

        if (user.StudentId is Guid studentId)
            identity.AddClaim(new Claim("student_id", studentId.ToString("D")));

        if (user.RecruiterId is Guid recruiterId)
            identity.AddClaim(new Claim("recruiter_id", recruiterId.ToString("D")));

        return identity;
    }
}
