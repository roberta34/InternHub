using InternHub.Api.Domain.Auth;
using Microsoft.AspNetCore.Identity;

namespace InternHub.Api.Infrastructure.Auth;

public static class RecruiterSeeder
{
    private const string RecruiterRole = "Recruiter";

    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment,
        CancellationToken ct = default)
    {
        if (!environment.IsDevelopment() && !environment.IsEnvironment("Testing"))
            return;

        var email = configuration["Seed:Recruiter:Email"];
        var password = configuration["Seed:Recruiter:Password"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            return;

        ct.ThrowIfCancellationRequested();
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();

        if (!await roles.RoleExistsAsync(RecruiterRole))
        {
            var createRoleResult = await roles.CreateAsync(new IdentityRole<Guid>
            {
                Id = Guid.NewGuid(),
                Name = RecruiterRole
            });
            EnsureSucceeded(createRoleResult, "create the Recruiter role");
        }

        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = email,
                Email = email,
                RecruiterId = Guid.NewGuid()
            };

            var createUserResult = await users.CreateAsync(user, password);
            EnsureSucceeded(createUserResult, "create the configured recruiter account");
        }
        else
        {
            if (user.StudentId is not null)
                throw new InvalidOperationException(
                    "The configured recruiter email belongs to a student account.");

            if (user.RecruiterId is null)
            {
                user.RecruiterId = Guid.NewGuid();
                var updateUserResult = await users.UpdateAsync(user);
                EnsureSucceeded(updateUserResult, "update the configured recruiter account");
            }
        }

        if (!await users.IsInRoleAsync(user, RecruiterRole))
        {
            var addRoleResult = await users.AddToRoleAsync(user, RecruiterRole);
            EnsureSucceeded(addRoleResult, "assign the Recruiter role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(" ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Failed to {operation}: {errors}");
        }
    }
}
