using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using FluentAssertions;
using InternHub.Api.Domain.Notifications;
using InternHub.Api.Features.Notifications.GetRecruiterNotifications;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InternHub.Tests.Integration.Notifications.GetRecruiterNotifications;

[Collection("Integration")]
public sealed class GetRecruiterNotificationsEndpointTests
{
    private const string TestAuthScheme = "TestRecruiterInboxAuth";
    private const string AuthenticatedHeader = "X-Test-Authenticated";
    private const string RoleHeader = "X-Test-Role";
    private const string RecruiterIdHeader = "X-Test-RecruiterId";
    private const string SubjectHeader = "X-Test-Sub";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    private static readonly DateTime BaseTimestampUtc =
        new(2027, 5, 10, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_DefaultProductionRegistrationAndUnauthenticatedRequest_When_GettingNotifications_Then_ReturnsUnauthorizedWithoutServerError()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var notification = CreateNotification(Guid.NewGuid(), BaseTimestampUtc);
        await SeedNotificationsAsync(factory, notification);

        // Act
        using var response = await client.GetAsync("/api/v1/recruiters/me/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadAllNotificationsAsync(factory))
            .Should().ContainSingle()
            .Which.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task Given_DefaultFailClosedCompanyLookupAndAuthenticatedRecruiter_When_GettingNotifications_Then_ReturnsForbiddenWithoutExposingData()
    {
        // Arrange
        using var factory = CreateFactoryWithTestAuthenticationOnly();
        using var client = factory.CreateClient();
        var notification = CreateNotification(Guid.NewGuid(), BaseTimestampUtc);
        await SeedNotificationsAsync(factory, notification);
        using var request = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: Guid.NewGuid().ToString(),
            subjectId: Guid.NewGuid().ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadAllNotificationsAsync(factory))
            .Should().ContainSingle()
            .Which.Id.Should().Be(notification.Id);
    }

    [Fact]
    public async Task Given_UnauthenticatedRequestWithTestAuthPipeline_When_GettingNotifications_Then_ReturnsUnauthorized()
    {
        // Arrange
        var lookup = new ControllableRecruiterCompanyAccessLookup();
        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        await SeedNotificationsAsync(factory, CreateNotification(Guid.NewGuid(), BaseTimestampUtc));

        // Act
        using var response = await client.GetAsync("/api/v1/recruiters/me/notifications");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await ReadAllNotificationsAsync(factory)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_AuthenticatedStudent_When_GettingNotifications_Then_ReturnsForbiddenAndLeavesNotificationsUnchanged()
    {
        // Arrange
        var lookup = new ControllableRecruiterCompanyAccessLookup();
        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        var notification = CreateNotification(Guid.NewGuid(), BaseTimestampUtc);
        await SeedNotificationsAsync(factory, notification);
        using var request = CreateAuthenticatedRequest(
            "Student",
            recruiterId: null,
            subjectId: Guid.NewGuid().ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadAllNotificationsAsync(factory))
            .Should().ContainSingle()
            .Which.Id.Should().Be(notification.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-valid-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_AuthenticatedRecruiterWithInvalidRecruiterId_When_GettingNotifications_Then_ReturnsForbidden(
        string? invalidRecruiterId)
    {
        // Arrange
        var lookup = new ControllableRecruiterCompanyAccessLookup();
        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        await SeedNotificationsAsync(factory, CreateNotification(Guid.NewGuid(), BaseTimestampUtc));
        using var request = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: invalidRecruiterId,
            subjectId: Guid.NewGuid().ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await ReadAllNotificationsAsync(factory)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_TwoRecruitersFromCompanyA_When_BothGetNotifications_Then_BothReceiveSameCompanyANotificationsInIdenticalOrderWithoutOtherCompanyData()
    {
        // Arrange
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var recruiterA1 = Guid.NewGuid();
        var recruiterA2 = Guid.NewGuid();
        var accountA1 = Guid.NewGuid();
        var accountA2 = Guid.NewGuid();
        var offerA1 = Guid.NewGuid();
        var offerA2 = Guid.NewGuid();
        var offerB = Guid.NewGuid();

        var tiedTimestamp = BaseTimestampUtc.AddHours(1);
        var orderedTiedIds = new[] { Guid.NewGuid(), Guid.NewGuid() }
            .OrderBy(id => id.ToString(), StringComparer.Ordinal)
            .ToArray();

        var companyATiedSecond = CreateNotificationWithId(orderedTiedIds[1], offerA1, tiedTimestamp);
        var companyATiedFirst = CreateNotificationWithId(orderedTiedIds[0], offerA2, tiedTimestamp);
        var companyANewest = CreateNotification(offerA1, BaseTimestampUtc.AddHours(2));
        var companyBNotification = CreateNotification(offerB, BaseTimestampUtc.AddHours(3));

        var lookup = new ControllableRecruiterCompanyAccessLookup();
        lookup.AssignRecruiterToCompany(recruiterA1, companyA);
        lookup.AssignRecruiterToCompany(recruiterA2, companyA);
        lookup.AssignOffersToCompany(companyA, offerA1, offerA2);
        lookup.AssignOffersToCompany(companyB, offerB);

        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        await SeedNotificationsAsync(
            factory,
            companyATiedSecond,
            companyBNotification,
            companyATiedFirst,
            companyANewest);

        using var firstRecruiterRequest = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: recruiterA1.ToString(),
            subjectId: accountA1.ToString());
        using var secondRecruiterRequest = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: recruiterA2.ToString(),
            subjectId: accountA2.ToString());

        // Act
        using var firstRecruiterResponse = await client.SendAsync(firstRecruiterRequest);
        using var secondRecruiterResponse = await client.SendAsync(secondRecruiterRequest);

        // Assert
        firstRecruiterResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondRecruiterResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstRawJson = await firstRecruiterResponse.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(firstRawJson);
        jsonDocument.RootElement.GetArrayLength().Should().Be(3);
        jsonDocument.RootElement[0].EnumerateObject()
            .Select(property => property.Name)
            .Should().BeEquivalentTo(
                ["id", "candidacyId", "offerId", "studentId", "kind", "occurredAtUtc"]);

        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var firstRecruiterItems = JsonSerializer.Deserialize<GetRecruiterNotificationsResponse[]>(
            firstRawJson,
            serializerOptions);
        var secondRecruiterItems = await secondRecruiterResponse.Content
            .ReadFromJsonAsync<GetRecruiterNotificationsResponse[]>();

        var expectedOrder = new[]
        {
            ToResponse(companyANewest),
            ToResponse(companyATiedFirst),
            ToResponse(companyATiedSecond)
        };

        firstRecruiterItems.Should().Equal(expectedOrder);
        secondRecruiterItems.Should().Equal(expectedOrder);
        firstRecruiterItems.Should().NotContain(item => item.OfferId == offerB);
        secondRecruiterItems.Should().NotContain(item => item.OfferId == offerB);

        var persisted = await ReadAllNotificationsAsync(factory);
        persisted.Should().HaveCount(4);
    }

    [Fact]
    public async Task Given_RecruiterFromCompanyB_When_GettingNotifications_Then_ReturnsOnlyCompanyBNotifications()
    {
        // Arrange
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var recruiterA = Guid.NewGuid();
        var recruiterB = Guid.NewGuid();
        var offerA = Guid.NewGuid();
        var offerB = Guid.NewGuid();

        var companyANotification = CreateNotification(offerA, BaseTimestampUtc.AddMinutes(30));
        var companyBNotification = CreateNotification(offerB, BaseTimestampUtc);

        var lookup = new ControllableRecruiterCompanyAccessLookup();
        lookup.AssignRecruiterToCompany(recruiterA, companyA);
        lookup.AssignRecruiterToCompany(recruiterB, companyB);
        lookup.AssignOffersToCompany(companyA, offerA);
        lookup.AssignOffersToCompany(companyB, offerB);

        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        await SeedNotificationsAsync(factory, companyANotification, companyBNotification);

        using var request = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: recruiterB.ToString(),
            subjectId: Guid.NewGuid().ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<GetRecruiterNotificationsResponse[]>();
        items.Should().ContainSingle()
            .Which.Should().Be(ToResponse(companyBNotification));
        items.Should().NotContain(item => item.OfferId == offerA);

        (await ReadAllNotificationsAsync(factory)).Should().HaveCount(2);
    }

    [Fact]
    public async Task Given_AuthorizedRecruiterWithEmptyInbox_When_GettingNotifications_Then_ReturnsOkWithEmptyList()
    {
        // Arrange
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var recruiterA = Guid.NewGuid();
        var offerA = Guid.NewGuid();
        var offerB = Guid.NewGuid();

        var lookup = new ControllableRecruiterCompanyAccessLookup();
        lookup.AssignRecruiterToCompany(recruiterA, companyA);
        lookup.AssignOffersToCompany(companyA, offerA);
        lookup.AssignOffersToCompany(companyB, offerB);

        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        await SeedNotificationsAsync(factory, CreateNotification(offerB, BaseTimestampUtc));

        using var request = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: recruiterA.ToString(),
            subjectId: Guid.NewGuid().ToString());

        // Act
        using var response = await client.SendAsync(request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await response.Content.ReadFromJsonAsync<GetRecruiterNotificationsResponse[]>();
        items.Should().BeEmpty();
        (await ReadAllNotificationsAsync(factory)).Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_PersistedNotifications_When_ReadingInboxRepeatedly_Then_NotificationsRemainUnchangedInSqlite()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var accountId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var notification = CreateNotification(offerId, BaseTimestampUtc);

        var lookup = new ControllableRecruiterCompanyAccessLookup();
        lookup.AssignRecruiterToCompany(recruiterId, companyId);
        lookup.AssignOffersToCompany(companyId, offerId);

        using var factory = CreateFactory(lookup);
        using var client = factory.CreateClient();
        await SeedNotificationsAsync(factory, notification);

        using var firstRequest = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: recruiterId.ToString(),
            subjectId: accountId.ToString());
        using var secondRequest = CreateAuthenticatedRequest(
            "Recruiter",
            recruiterId: recruiterId.ToString(),
            subjectId: accountId.ToString());

        // Act
        using var firstResponse = await client.SendAsync(firstRequest);
        using var secondResponse = await client.SendAsync(secondRequest);

        // Assert
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstItems = await firstResponse.Content.ReadFromJsonAsync<GetRecruiterNotificationsResponse[]>();
        var secondItems = await secondResponse.Content.ReadFromJsonAsync<GetRecruiterNotificationsResponse[]>();
        firstItems.Should().Equal(ToResponse(notification));
        secondItems.Should().Equal(ToResponse(notification));

        var persisted = (await ReadAllNotificationsAsync(factory))
            .Should().ContainSingle().Subject;
        persisted.Id.Should().Be(notification.Id);
        persisted.CandidacyId.Should().Be(notification.CandidacyId);
        persisted.OfferId.Should().Be(notification.OfferId);
        persisted.StudentId.Should().Be(notification.StudentId);
        persisted.Kind.Should().Be(notification.Kind);
        persisted.OccurredAtUtc.Should().Be(notification.OccurredAtUtc);
    }

    private static InternHubSqliteFactory CreateFactory(
        IRecruiterCompanyAccessLookup companyLookup) =>
        new(configureTestServices: services =>
        {
            ConfigureTestAuthentication(services);
            services.RemoveAll<IRecruiterCompanyAccessLookup>();
            services.AddSingleton(companyLookup);
        });

    private static InternHubSqliteFactory CreateFactoryWithTestAuthenticationOnly() =>
        new(configureTestServices: ConfigureTestAuthentication);

    private static void ConfigureTestAuthentication(IServiceCollection services)
    {
        services
            .AddAuthentication(TestAuthScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                TestAuthScheme,
                _ => { });
        services.AddTransient<IStartupFilter, TestAuthenticationStartupFilter>();
    }

    private static HttpRequestMessage CreateAuthenticatedRequest(
        string role,
        string? recruiterId,
        string? subjectId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/recruiters/me/notifications");
        request.Headers.Add(AuthenticatedHeader, "true");
        request.Headers.Add(RoleHeader, role);

        if (recruiterId is not null)
        {
            request.Headers.Add(RecruiterIdHeader, recruiterId);
        }

        if (subjectId is not null)
        {
            request.Headers.Add(SubjectHeader, subjectId);
        }

        return request;
    }

    private static NotificationOutboxEntry CreateNotification(Guid offerId, DateTime occurredAtUtc) =>
        CreateNotificationWithId(Guid.NewGuid(), offerId, occurredAtUtc);

    private static NotificationOutboxEntry CreateNotificationWithId(
        Guid id,
        Guid offerId,
        DateTime occurredAtUtc) =>
        NotificationOutboxEntry.ShortlistedCandidacyWithdrawn(
            id,
            Guid.NewGuid(),
            offerId,
            Guid.NewGuid(),
            occurredAtUtc);

    private static GetRecruiterNotificationsResponse ToResponse(NotificationOutboxEntry entry) =>
        new(
            entry.Id,
            entry.CandidacyId,
            entry.OfferId,
            entry.StudentId,
            entry.Kind,
            entry.OccurredAtUtc);

    private static async Task SeedNotificationsAsync(
        InternHubSqliteFactory factory,
        params NotificationOutboxEntry[] notifications)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.NotificationOutboxEntries.AddRange(notifications);
        await db.SaveChangesAsync();
    }

    private static async Task<List<NotificationOutboxEntry>> ReadAllNotificationsAsync(
        InternHubSqliteFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        return await db.NotificationOutboxEntries
            .AsNoTracking()
            .ToListAsync();
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

            if (Request.Headers.TryGetValue(RecruiterIdHeader, out var recruiterId))
            {
                claims.Add(new Claim(RecruiterIdClaimType, recruiterId.ToString()));
            }

            if (Request.Headers.TryGetValue(SubjectHeader, out var subjectId))
            {
                claims.Add(new Claim(SubjectClaimType, subjectId.ToString()));
            }

            var identity = new ClaimsIdentity(claims, TestAuthScheme);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, TestAuthScheme);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    private sealed class ControllableRecruiterCompanyAccessLookup : IRecruiterCompanyAccessLookup
    {
        private readonly Dictionary<Guid, Guid> _recruiterCompanies = [];
        private readonly Dictionary<Guid, List<Guid>> _companyOffers = [];

        public void AssignRecruiterToCompany(Guid recruiterId, Guid companyId) =>
            _recruiterCompanies[recruiterId] = companyId;

        public void AssignOffersToCompany(Guid companyId, params Guid[] offerIds) =>
            _companyOffers[companyId] = [.. offerIds];

        public Task<Guid?> GetRecruiterCompanyIdAsync(
            Guid recruiterId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult<Guid?>(
                _recruiterCompanies.TryGetValue(recruiterId, out var companyId)
                    ? companyId
                    : null);
        }

        public Task<IReadOnlyList<Guid>> GetCompanyOfferIdsAsync(
            Guid companyId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Guid> offerIds = _companyOffers.TryGetValue(companyId, out var offers)
                ? offers.ToArray()
                : [];
            return Task.FromResult(offerIds);
        }
    }
}
