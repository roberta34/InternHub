using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Offers.UpdateOffer;

public sealed class UpdateOfferEndpointTests
{
    private static readonly DateTime InitialDeadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime UpdatedDeadline =
        new(2027, 2, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ExistingOfferWithCurrentVersion_When_UpdatingOffer_Then_ReturnsNoContentAndPersistsVersionIncrement()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = await CreateOffer(client);
        var request = new
        {
            title = "Updated Backend Intern",
            companyName = "Updated Company",
            requirements = "C#, EF Core",
            deadline = UpdatedDeadline,
            maximumAvailableSlots = 4,
            version = offer.Version
        };

        // Act
        using var response = await client.PutAsJsonAsync($"/api/v1/offers/{offer.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking().SingleAsync(item => item.Id == offer.Id);
        persisted.Title.Should().Be("Updated Backend Intern");
        persisted.CompanyName.Should().Be("Updated Company");
        persisted.Requirements.Should().Be("C#, EF Core");
        persisted.Deadline.Should().Be(UpdatedDeadline);
        persisted.MaximumAvailableSlots.Should().Be(4);
        persisted.Version.Should().Be(offer.Version + 1);
    }

    [Fact]
    public async Task Given_InvalidOfferData_When_UpdatingOffer_Then_ReturnsBadRequestAndPreservesPersistedData()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = await CreateOffer(client);
        var request = new
        {
            title = "",
            companyName = "Updated Company",
            requirements = "C#, EF Core",
            deadline = UpdatedDeadline,
            maximumAvailableSlots = 4,
            version = offer.Version
        };

        // Act
        using var response = await client.PutAsJsonAsync($"/api/v1/offers/{offer.Id}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("Title is required.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking().SingleAsync(item => item.Id == offer.Id);
        persisted.Title.Should().Be(offer.Title);
        persisted.CompanyName.Should().Be(offer.CompanyName);
        persisted.Requirements.Should().Be(offer.Requirements);
        persisted.Deadline.Should().Be(offer.Deadline);
        persisted.MaximumAvailableSlots.Should().Be(offer.MaximumAvailableSlots);
        persisted.Version.Should().Be(offer.Version);
    }

    [Fact]
    public async Task Given_MissingOffer_When_UpdatingOffer_Then_ReturnsNotFoundAndLeavesDatabaseEmpty()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offerId = Guid.NewGuid();
        var request = new
        {
            title = "Updated Backend Intern",
            companyName = "Updated Company",
            requirements = "C#, EF Core",
            deadline = UpdatedDeadline,
            maximumAvailableSlots = 4,
            version = 0
        };

        // Act
        using var response = await client.PutAsJsonAsync($"/api/v1/offers/{offerId}", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain(offerId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_OfferUpdatedWithCurrentVersion_When_UpdatingWithStaleVersion_Then_ReturnsConflictAndPreservesLatestData()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = await CreateOffer(client);
        var firstUpdate = new
        {
            title = "Latest Backend Intern",
            companyName = "Latest Company",
            requirements = "C#, EF Core",
            deadline = UpdatedDeadline,
            maximumAvailableSlots = 4,
            version = offer.Version
        };
        using var firstResponse = await client.PutAsJsonAsync($"/api/v1/offers/{offer.Id}", firstUpdate);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var staleUpdate = new
        {
            title = "Stale Backend Intern",
            companyName = "Stale Company",
            requirements = "Old requirements",
            deadline = InitialDeadline,
            maximumAvailableSlots = 1,
            version = offer.Version
        };

        // Act
        using var response = await client.PutAsJsonAsync($"/api/v1/offers/{offer.Id}", staleUpdate);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("stale version");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking().SingleAsync(item => item.Id == offer.Id);
        persisted.Title.Should().Be("Latest Backend Intern");
        persisted.CompanyName.Should().Be("Latest Company");
        persisted.Requirements.Should().Be("C#, EF Core");
        persisted.Deadline.Should().Be(UpdatedDeadline);
        persisted.MaximumAvailableSlots.Should().Be(4);
        persisted.Version.Should().Be(offer.Version + 1);
    }

    private static async Task<OfferResponse> CreateOffer(HttpClient client)
    {
        var request = new
        {
            title = "Backend Intern",
            companyName = "Example Company",
            requirements = "C#, Git",
            deadline = InitialDeadline,
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
