using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.ReviewCandidacyWithAi;

[Collection("Integration")]
public sealed class ReviewCandidacyWithAiEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ExistingCandidacyAndBaselineProvider_When_RequestingAiReview_Then_ReturnsOkWithProviderHeaders()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = new Student(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");
        var offer = new Offer("Offer", "Company", "C#, SQL", ApplicationDate.AddDays(10), 1);
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(), student.Id, offer.Id, ApplicationDate, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, student, offer, candidacy);

        // Act
        using var response = await client.PostAsJsonAsync(
            $"/api/v1/candidacies/{candidacy.Id}/ai-review",
            new ReviewCandidacyWithAiRequest("Built REST APIs in C# backed by SQL."));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-InternHub-AI-Provider").Should().Equal("Baseline");
        response.Headers.GetValues("X-InternHub-AI-Fallback").Should().Equal("false");
        (await response.Content.ReadFromJsonAsync<ReviewCandidacyWithAiResponse>()).Should().NotBeNull();
    }

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        Student student,
        Offer offer,
        InternshipCandidacy candidacy)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Students.Add(student);
        db.Offers.Add(offer);
        db.InternshipCandidacies.Add(candidacy);

        await db.SaveChangesAsync();
    }
}
