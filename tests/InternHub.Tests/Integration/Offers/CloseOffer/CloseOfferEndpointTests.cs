using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Offers.CloseOffer;

[Collection("Integration")]
public sealed class CloseOfferEndpointTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ExistingOpenOffer_When_ClosingOffer_Then_ReturnsNoContentAndPersistsClosedOfferWithSingleVersionIncrement()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = await CreateOffer(client);

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/offers/{offer.Id}/close",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking()
            .SingleAsync(item => item.Id == offer.Id);
        persisted.IsClosed.Should().BeTrue();
        persisted.Version.Should().Be(offer.Version + 1);
    }

    [Fact]
    public async Task Given_AlreadyClosedOffer_When_ClosingOfferAgain_Then_ReturnsNoContentAndPreservesVersion()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = await CreateOffer(client);
        using var firstResponse = await client.PostAsync(
            $"/api/v1/offers/{offer.Id}/close",
            content: null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/offers/{offer.Id}/close",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking()
            .SingleAsync(item => item.Id == offer.Id);
        persisted.IsClosed.Should().BeTrue();
        persisted.Version.Should().Be(offer.Version + 1);
    }

    [Fact]
    public async Task Given_MissingOffer_When_ClosingOffer_Then_ReturnsNotFoundAndLeavesDatabaseUnchanged()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offerId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/offers/{offerId}/close",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync())
            .Should().Contain($"Offer {offerId} not found.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_OpenOfferWithExistingFieldsAndCandidacy_When_ClosingOffer_Then_PreservesFieldsAndCandidacy()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = await CreateOffer(client);
        var student = new Student(
            Guid.NewGuid(),
            "Intern Hub Student",
            $"student-{Guid.NewGuid():N}@example.com");
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            offer.Id,
            new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc),
            InternshipCandidacyStatus.Submitted);
        using (var seedScope = factory.Services.CreateScope())
        {
            var seedDb = seedScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
            seedDb.Students.Add(student);
            seedDb.InternshipCandidacies.Add(candidacy);
            await seedDb.SaveChangesAsync();
        }

        // Act
        using var response = await client.PostAsync(
            $"/api/v1/offers/{offer.Id}/close",
            content: null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persistedOffer = await db.Offers.AsNoTracking()
            .SingleAsync(item => item.Id == offer.Id);
        persistedOffer.Title.Should().Be(offer.Title);
        persistedOffer.CompanyName.Should().Be(offer.CompanyName);
        persistedOffer.Requirements.Should().Be(offer.Requirements);
        persistedOffer.Deadline.Should().Be(offer.Deadline);
        persistedOffer.MaximumAvailableSlots.Should().Be(offer.MaximumAvailableSlots);
        persistedOffer.IsClosed.Should().BeTrue();
        persistedOffer.Version.Should().Be(offer.Version + 1);

        var persistedCandidacy = await db.InternshipCandidacies.AsNoTracking()
            .SingleAsync(item => item.Id == candidacy.Id);
        persistedCandidacy.Should().BeEquivalentTo(candidacy);
    }

    private static async Task<OfferResponse> CreateOffer(HttpClient client)
    {
        var request = new
        {
            title = "Backend Intern",
            companyName = "Example Company",
            requirements = "C#, Git",
            deadline = Deadline,
            maximumAvailableSlots = 2
        };

        using var response = await client.PostAsJsonAsync("/api/v1/offers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OfferResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    private sealed record OfferResponse(
        Guid Id,
        string Title,
        string CompanyName,
        string Requirements,
        DateTime Deadline,
        int MaximumAvailableSlots,
        bool IsClosed,
        int Version);
}
