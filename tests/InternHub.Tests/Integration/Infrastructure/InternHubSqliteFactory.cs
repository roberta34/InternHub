using InternHub.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace InternHub.Tests.Integration.Infrastructure;

public sealed class InternHubSqliteFactory : WebApplicationFactory<Program>
{
    public static readonly Guid RecruiterId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly SqliteConnection _connection;
    private readonly TimeProvider _timeProvider;
    private readonly bool _useSeparateConnections;
    private readonly string? _sharedMemoryConnectionString;
    private readonly Action<IServiceCollection>? _configureTestServices;
    private readonly bool _authenticateRecruiter;
    private readonly Guid _recruiterId;

    public InternHubSqliteFactory(
        TimeProvider? timeProvider = null,
        bool useSeparateConnections = false,
        Action<IServiceCollection>? configureTestServices = null,
        bool authenticateRecruiter = true,
        Guid? recruiterId = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _useSeparateConnections = useSeparateConnections;
        _configureTestServices = configureTestServices;
        _authenticateRecruiter = authenticateRecruiter;
        _recruiterId = recruiterId ?? RecruiterId;
        _sharedMemoryConnectionString = useSeparateConnections
            ? new SqliteConnectionStringBuilder
            {
                DataSource = $"InternHub-{Guid.NewGuid():N}",
                Mode = SqliteOpenMode.Memory,
                Cache = SqliteCacheMode.Shared,
                DefaultTimeout = 1,
                Pooling = false
            }.ToString()
            : null;
        _connection = new SqliteConnection(
            _sharedMemoryConnectionString ?? "Data Source=:memory:");
        _connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<InternHubDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<InternHubDbContext>>();
            services.AddDbContext<InternHubDbContext>(options =>
            {
                if (_useSeparateConnections)
                    options.UseSqlite(_sharedMemoryConnectionString!);
                else
                    options.UseSqlite(_connection);
            });
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(_timeProvider);
            services.AddAuthentication("Test")
                .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                    "Test",
                    _ => { });
            services.AddAuthorization();
            services.AddSingleton(new TestAuthenticationOptions(
                _authenticateRecruiter,
                _recruiterId));
            _configureTestServices?.Invoke(services);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _connection.Dispose();
    }

    private sealed record TestAuthenticationOptions(
        bool IsAuthenticated,
        Guid RecruiterId);

    private sealed class TestAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TestAuthenticationOptions authentication)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!authentication.IsAuthenticated)
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, authentication.RecruiterId.ToString()),
                new Claim("recruiter_id", authentication.RecruiterId.ToString()),
                new Claim(ClaimTypes.Role, "Recruiter")
            };
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
