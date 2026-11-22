using InternHub.Api.Domain.Auth;
using InternHub.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;

namespace InternHub.Api.Infrastructure.Auth;

public static class AuthExtensions
{
    public static IServiceCollection AddAuthFeatures(this IServiceCollection services)
    {
        services.AddIdentityCore<ApplicationUser>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.ClaimsIdentity.RoleClaimType = "role";
        })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<InternHubDbContext>()
            .AddClaimsPrincipalFactory<InternHubClaimsFactory>();

        services.AddAuthentication(IdentityConstants.BearerScheme)
            .AddBearerToken(IdentityConstants.BearerScheme);
        services.AddAuthorization();
        services.AddScoped<IStudentAccountRegistration, IdentityStudentAccountRegistration>();
        services.AddScoped<IAccountAuthenticator, IdentityAccountAuthenticator>();

        return services;
    }
}
