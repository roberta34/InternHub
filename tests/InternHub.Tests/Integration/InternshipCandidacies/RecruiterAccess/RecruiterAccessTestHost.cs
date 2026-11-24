using System.Security.Claims;
using System.Text.Encodings.Web;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InternHub.Tests.Integration.InternshipCandidacies.RecruiterAccess;

/// <summary>
/// Builds an <see cref="InternHubSqliteFactory"/> (used, not changed) whose caller has exactly the
/// given claims (null = anonymous), with every log message captured and optional service overrides.
/// </summary>
internal static class RecruiterAccessTestHost
{
    public const string SchemeName = "RecruiterAccessTest";

    public static Claim[] Recruiter(Guid recruiterId) =>
    [
        new(ClaimTypes.Role, "Recruiter"),
        new("recruiter_id", recruiterId.ToString("D"))
    ];

    public static Claim[] Student(Guid studentId) =>
    [
        new(ClaimTypes.Role, "Student"),
        new("student_id", studentId.ToString("D"))
    ];

    public static InternHubSqliteFactory CreateFactory(
        IReadOnlyList<Claim>? claims,
        CapturingLoggerProvider logs,
        Action<IServiceCollection>? configureServices = null) =>
        new(configureTestServices: services =>
        {
            services.AddSingleton(new TestCaller(claims));
            services
                .AddAuthentication(SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestCallerAuthenticationHandler>(SchemeName, _ => { });
            AddLogCapture(services, logs);
            configureServices?.Invoke(services);
        });

    public static void AddLogCapture(IServiceCollection services, CapturingLoggerProvider logs)
    {
        services.AddSingleton<ILoggerProvider>(logs);
        services.AddLogging(builder => builder.AddFilter<CapturingLoggerProvider>(_ => true));
    }

    private sealed record TestCaller(IReadOnlyList<Claim>? Claims);

    private sealed class TestCallerAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestCaller caller) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (caller.Claims is null)
                return Task.FromResult(AuthenticateResult.NoResult());

            var principal = new ClaimsPrincipal(new ClaimsIdentity(caller.Claims, SchemeName));
            return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(principal, SchemeName)));
        }
    }
}
