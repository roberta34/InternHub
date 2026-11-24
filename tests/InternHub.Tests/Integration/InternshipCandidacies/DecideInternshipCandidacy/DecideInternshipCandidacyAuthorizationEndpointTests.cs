using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using InternHub.Tests.Integration.InternshipCandidacies.RecruiterAccess;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.DecideInternshipCandidacy;

[Collection("Integration")]
public sealed class DecideInternshipCandidacyAuthorizationEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public async Task Given_OwningRecruiter_When_Accepting_Then_ReturnsOkAndPersistsAccepted()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(OwnerId));

        // Act
        using var response = await host.PostDecisionAsync("Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var state = await host.ReadStateAsync();
        state.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        state.OfferVersion.Should().Be(host.OfferVersionBefore + 1);
    }

    [Fact]
    public async Task Given_AnonymousCaller_When_Accepting_Then_ReturnsUnauthorizedAndChangesNothing()
    {
        // Arrange
        using var host = await ArrangeAsync(claims: null);

        // Act
        using var response = await host.PostDecisionAsync("Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await host.ReadStateAsync()).Should().Be(host.UnchangedState);
    }

    [Fact]
    public async Task Given_Student_When_Accepting_Then_ReturnsForbiddenAndChangesNothing()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Student(Guid.NewGuid()));

        // Act
        using var response = await host.PostDecisionAsync("Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await host.ReadStateAsync()).Should().Be(host.UnchangedState);
    }

    [Fact]
    public async Task Given_AnotherRecruiter_When_Accepting_Then_ReturnsForbiddenAndChangesNothing()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(Guid.NewGuid()));

        // Act
        using var response = await host.PostDecisionAsync("Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await host.ReadStateAsync()).Should().Be(host.UnchangedState);
    }

    private static async Task<ArrangedHost> ArrangeAsync(IReadOnlyList<Claim>? claims)
    {
        var factory = RecruiterAccessTestHost.CreateFactory(claims, new CapturingLoggerProvider());
        var student = new Student(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");
        var offer = new Offer("Offer", "Company", ".NET", ApplicationDate.AddDays(10), 1, OwnerId);
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(), student.Id, offer.Id, ApplicationDate, InternshipCandidacyStatus.Submitted);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
            db.Students.Add(student);
            db.Offers.Add(offer);
            db.InternshipCandidacies.Add(candidacy);
            await db.SaveChangesAsync();
        }

        var host = new ArrangedHost(factory, factory.CreateClient(), candidacy.Id, offer.Id);
        host.UnchangedState = await host.ReadStateAsync();
        return host;
    }

    private sealed record PersistedState(InternshipCandidacyStatus Status, int OfferVersion, int AcceptedCount);

    private sealed class ArrangedHost(
        InternHubSqliteFactory factory,
        HttpClient client,
        Guid candidacyId,
        Guid offerId) : IDisposable
    {
        public PersistedState UnchangedState { get; set; } = null!;

        public int OfferVersionBefore => UnchangedState.OfferVersion;

        public Task<HttpResponseMessage> PostDecisionAsync(string action) =>
            client.PostAsJsonAsync(
                $"/api/v1/candidacies/{candidacyId}/decision",
                new DecideInternshipCandidacyRequest(action));

        public async Task<PersistedState> ReadStateAsync()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
            var status = await db.InternshipCandidacies.AsNoTracking()
                .Where(candidacy => candidacy.Id == candidacyId)
                .Select(candidacy => candidacy.Status)
                .SingleAsync();
            var version = await db.Offers.AsNoTracking()
                .Where(offer => offer.Id == offerId)
                .Select(offer => offer.Version)
                .SingleAsync();
            var accepted = await db.InternshipCandidacies.AsNoTracking()
                .CountAsync(candidacy =>
                    candidacy.OfferId == offerId &&
                    candidacy.Status == InternshipCandidacyStatus.Accepted);

            return new PersistedState(status, version, accepted);
        }

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
        }
    }
}
