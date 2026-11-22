using Microsoft.AspNetCore.Identity;

namespace InternHub.Api.Domain.Auth;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid? StudentId { get; set; }
    public Guid? RecruiterId { get; set; }
}
