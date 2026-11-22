using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using InternHub.Api.Domain.Auth;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.RecruiterAccess;

/// <summary>
/// Proves the recruiter guard accepts the claims Identity really issues: the recruiter is created by
/// RecruiterSeeder (Testing environment), logs in through /auth/login and uses the bearer token.
/// </summary>
[Collection("Integration")]
public sealed class IdentityBearerTokenRecruiterAccessEndpointTests
{
    private const string Email = "seeded-recruiter@example.test";
    private const string Password = "Recruiter#Test2027";

    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_SeededRecruiterBearerToken_When_RequestingAiReviewOfOwnCandidacy_Then_ReturnsOk()
    {
        // Arrange
        using var baseFactory = CreateBaseFactory();
        using var factory = WithSeededRecruiter(baseFactory);
        using var client = factory.CreateClient();
        var candidacyId = await SeedOwnCandidacyAsync(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/candidacies/{candidacyId}/ai-review",
            new ReviewCandidacyWithAiRequest("Built REST APIs in C#."));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Given_SeededRecruiterBearerToken_When_DecidingOwnCandidacy_Then_ReturnsOk()
    {
        // Arrange
        using var baseFactory = CreateBaseFactory();
        using var factory = WithSeededRecruiter(baseFactory);
        using var client = factory.CreateClient();
        var candidacyId = await SeedOwnCandidacyAsync(factory);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", await LoginAsync(client));

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/candidacies/{candidacyId}/decision",
            new DecideInternshipCandidacyRequest("Shortlist"));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static InternHubSqliteFactory CreateBaseFactory() =>
        new(
            authenticateRecruiter: false,
            configureTestServices: services =>
                services.AddAuthentication(IdentityConstants.BearerScheme));

    private static WebApplicationFactory<Program> WithSeededRecruiter(InternHubSqliteFactory factory) =>
        factory.WithWebHostBuilder(builder => builder
            .UseSetting("Seed:Recruiter:Email", Email)
            .UseSetting("Seed:Recruiter:Password", Password));

    private static async Task<Guid> SeedOwnCandidacyAsync(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var recruiter = await users.FindByEmailAsync(Email);
        recruiter.Should().NotBeNull("RecruiterSeeder must create the configured recruiter in Testing");
        recruiter!.RecruiterId.Should().NotBeNull();

        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var student = new Student(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");
        var offer = new Offer("Offer", "Company", "C#", ApplicationDate.AddDays(10), 1, recruiter.RecruiterId);
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(), student.Id, offer.Id, ApplicationDate, InternshipCandidacyStatus.Submitted);
        db.Students.Add(student);
        db.Offers.Add(offer);
        db.InternshipCandidacies.Add(candidacy);
        await db.SaveChangesAsync();

        return candidacy.Id;
    }

    private static async Task<string> LoginAsync(HttpClient client)
    {
        using var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { email = Email, password = Password });
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return json.RootElement.GetProperty("accessToken").GetString()!;
    }
}
