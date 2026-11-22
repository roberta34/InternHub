using System.Security.Claims;
using System.Text.Encodings.Web;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InternHub.Tests.Integration.InternshipCandidacies;

internal static class StudentTestAuthentication
{
    public const string SchemeName = "TestStudentAccessAuth";
    private const string AuthenticatedHeader = "X-Test-Authenticated";
    private const string RoleHeader = "X-Test-Role";
    private const string StudentIdHeader = "X-Test-StudentId";
    private const string RecruiterIdHeader = "X-Test-RecruiterId";
    private const string SubjectHeader = "X-Test-Sub";
    private const string StudentIdClaimType = "student_id";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    public static InternHubSqliteFactory CreateFactory(TimeProvider? timeProvider = null) =>
        new(
            timeProvider: timeProvider,
            configureTestServices: services =>
            {
                services
                    .AddAuthentication(SchemeName)
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        SchemeName,
                        _ => { });
                services.AddTransient<IStartupFilter, TestAuthenticationStartupFilter>();
            });

    public static HttpRequestMessage CreateStudentGetRequest(
        string path,
        string? studentId,
        string? subjectId = null) =>
        CreateStudentRequest(HttpMethod.Get, path, studentId, subjectId);

    public static HttpRequestMessage CreateStudentPostRequest(
        string path,
        string? studentId,
        string? subjectId = null) =>
        CreateStudentRequest(HttpMethod.Post, path, studentId, subjectId);

    public static HttpRequestMessage CreateRecruiterGetRequest(
        string path,
        Guid recruiterId,
        string? subjectId = null) =>
        CreateRecruiterRequest(HttpMethod.Get, path, recruiterId, subjectId);

    public static HttpRequestMessage CreateRecruiterPostRequest(
        string path,
        Guid recruiterId,
        string? subjectId = null) =>
        CreateRecruiterRequest(HttpMethod.Post, path, recruiterId, subjectId);

    private static HttpRequestMessage CreateStudentRequest(
        HttpMethod method,
        string path,
        string? studentId,
        string? subjectId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(AuthenticatedHeader, "true");
        request.Headers.Add(RoleHeader, "Student");

        if (studentId is not null)
        {
            request.Headers.Add(StudentIdHeader, studentId);
        }

        if (subjectId is not null)
        {
            request.Headers.Add(SubjectHeader, subjectId);
        }

        return request;
    }

    private static HttpRequestMessage CreateRecruiterRequest(
        HttpMethod method,
        string path,
        Guid recruiterId,
        string? subjectId)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(AuthenticatedHeader, "true");
        request.Headers.Add(RoleHeader, "Recruiter");
        request.Headers.Add(RecruiterIdHeader, recruiterId.ToString());

        if (subjectId is not null)
        {
            request.Headers.Add(SubjectHeader, subjectId);
        }

        return request;
    }

    private sealed class TestAuthenticationStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            app =>
            {
                app.UseAuthentication();
                next(app);
            };
    }

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(AuthenticatedHeader, out var authenticated) ||
                !string.Equals(authenticated.ToString(), "true", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var claims = new List<Claim>();

            if (Request.Headers.TryGetValue(RoleHeader, out var role) &&
                !string.IsNullOrWhiteSpace(role.ToString()))
            {
                claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
            }

            if (Request.Headers.TryGetValue(StudentIdHeader, out var studentId))
            {
                claims.Add(new Claim(StudentIdClaimType, studentId.ToString()));
            }

            if (Request.Headers.TryGetValue(RecruiterIdHeader, out var recruiterId))
            {
                claims.Add(new Claim(RecruiterIdClaimType, recruiterId.ToString()));
            }

            if (Request.Headers.TryGetValue(SubjectHeader, out var subjectId))
            {
                claims.Add(new Claim(SubjectClaimType, subjectId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
