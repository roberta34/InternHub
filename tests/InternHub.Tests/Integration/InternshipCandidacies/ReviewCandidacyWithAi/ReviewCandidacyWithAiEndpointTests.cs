using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;
using InternHub.Api.Features.RecruiterReview;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using InternHub.Tests.Integration.InternshipCandidacies.RecruiterAccess;
using InternHub.Tests.Unit.RecruiterReview.Fakes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternHub.Tests.Integration.InternshipCandidacies.ReviewCandidacyWithAi;

[Collection("Integration")]
public sealed class ReviewCandidacyWithAiEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    private const string CvMarker = "CV-MARKER-4f1c9e";
    private const string Cv = CvMarker + " Built REST APIs in C# backed by SQL.";

    private static readonly Guid OwnerId = Guid.NewGuid();

    [Fact]
    public async Task Given_OwningRecruiter_When_RequestingAiReview_Then_ReturnsOkWithJsonShapeAndProviderHeaders()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(OwnerId), OwnerId);

        // Act
        using var response = await host.PostReviewAsync(Cv);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-InternHub-AI-Provider").Should().Equal("Baseline");
        response.Headers.GetValues("X-InternHub-AI-Fallback").Should().Equal("false");
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.EnumerateObject().Select(property => property.Name)
            .Should().Equal("matchedSkills", "missingOrUnclearSkills", "evidence", "summary");
        json.RootElement.GetProperty("matchedSkills").EnumerateArray().Select(item => item.GetString())
            .Should().BeEquivalentTo("C#", "SQL");
        json.RootElement.GetProperty("missingOrUnclearSkills").EnumerateArray().Select(item => item.GetString())
            .Should().Equal("Docker");
        var evidence = json.RootElement.GetProperty("evidence");
        evidence.EnumerateArray().Select(item => item.GetProperty("skill").GetString())
            .Should().BeEquivalentTo("C#", "SQL");
        evidence[0].EnumerateObject().Select(property => property.Name).Should().Equal("skill", "excerpt");
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    [Fact]
    public async Task Given_OwningRecruiterAndBlankCv_When_RequestingAiReview_Then_ReturnsBadRequest()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(OwnerId), OwnerId);

        // Act
        using var response = await host.PostReviewAsync("   ");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
    }

    [Fact]
    public async Task Given_AnonymousCaller_When_RequestingAiReview_Then_ReturnsUnauthorized()
    {
        // Arrange
        using var host = await ArrangeAsync(claims: null, OwnerId);

        // Act
        using var response = await host.PostReviewAsync(Cv);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    [Fact]
    public async Task Given_Student_When_RequestingAiReview_Then_ReturnsForbidden()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Student(Guid.NewGuid()), OwnerId);

        // Act
        using var response = await host.PostReviewAsync(Cv);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    [Fact]
    public async Task Given_AnotherRecruiter_When_RequestingAiReview_Then_ReturnsForbidden()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(Guid.NewGuid()), OwnerId);

        // Act
        using var response = await host.PostReviewAsync(Cv);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    [Fact]
    public async Task Given_LegacyOfferWithoutOwner_When_RequestingAiReview_Then_ReturnsForbidden()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(OwnerId), offerRecruiterId: null);

        // Act
        using var response = await host.PostReviewAsync(Cv);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_RequestingAiReview_Then_ReturnsNotFound()
    {
        // Arrange
        using var host = await ArrangeAsync(RecruiterAccessTestHost.Recruiter(OwnerId), OwnerId);

        // Act
        using var response = await host.PostReviewAsync(Cv, candidacyId: Guid.NewGuid());

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    [Fact]
    public async Task Given_UnavailableProvider_When_RequestingAiReview_Then_ReturnsServiceUnavailableWithoutCv()
    {
        // Arrange
        using var host = await ArrangeAsync(
            RecruiterAccessTestHost.Recruiter(OwnerId),
            OwnerId,
            services =>
            {
                services.RemoveAll<IRecruiterCandidacyReviewService>();
                services.AddSingleton<IRecruiterCandidacyReviewService>(
                    new FakeRecruiterCandidacyReviewService { SimulateUnavailable = true });
            });

        // Act
        using var response = await host.PostReviewAsync(Cv);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        (await response.Content.ReadAsStringAsync()).Should().NotContain(CvMarker);
        (await host.ReadStatusAsync()).Should().Be(InternshipCandidacyStatus.Submitted);
        host.Logs.Messages.Should().NotContain(message => message.Contains(CvMarker));
    }

    private static async Task<ArrangedHost> ArrangeAsync(
        IReadOnlyList<Claim>? claims,
        Guid? offerRecruiterId,
        Action<IServiceCollection>? configureServices = null)
    {
        var logs = new CapturingLoggerProvider();
        var factory = RecruiterAccessTestHost.CreateFactory(claims, logs, configureServices);
        var student = new Student(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");
        var offer = offerRecruiterId is null
            ? new Offer("Offer", "Company", "C#, SQL, Docker", ApplicationDate.AddDays(10), 1)
            : new Offer("Offer", "Company", "C#, SQL, Docker", ApplicationDate.AddDays(10), 1, offerRecruiterId);
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

        return new ArrangedHost(factory, factory.CreateClient(), candidacy.Id, logs);
    }

    private sealed class ArrangedHost(
        InternHubSqliteFactory factory,
        HttpClient client,
        Guid candidacyId,
        CapturingLoggerProvider logs) : IDisposable
    {
        public CapturingLoggerProvider Logs { get; } = logs;

        public Task<HttpResponseMessage> PostReviewAsync(string cvText, Guid? candidacyId = null) =>
            client.PostAsJsonAsync(
                $"/api/v1/candidacies/{candidacyId ?? CandidacyIdValue}/ai-review",
                new ReviewCandidacyWithAiRequest(cvText));

        public async Task<InternshipCandidacyStatus> ReadStatusAsync()
        {
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
            return await db.InternshipCandidacies.AsNoTracking()
                .Where(candidacy => candidacy.Id == CandidacyIdValue)
                .Select(candidacy => candidacy.Status)
                .SingleAsync();
        }

        private Guid CandidacyIdValue { get; } = candidacyId;

        public void Dispose()
        {
            client.Dispose();
            factory.Dispose();
        }
    }
}
