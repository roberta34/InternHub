using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Offers;

[Collection("Integration")]
public sealed class OfferEndpointTests
{
    private static readonly DateTime BackendDeadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime FrontendDeadline =
        new(2027, 2, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ValidOfferData_When_CreatingOffer_Then_ReturnsCreatedAndPersistsOffer()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var request = new CreateOfferRequest(
            "Backend Intern",
            "Example Company",
            "C#, Git",
            BackendDeadline,
            2);

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/offers", request);
        var created = await response.Content.ReadFromJsonAsync<OfferResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        created.Should().NotBeNull();
        created!.Id.Should().NotBe(Guid.Empty);
        created.Title.Should().Be(request.Title);
        created.CompanyName.Should().Be(request.CompanyName);
        created.Requirements.Should().Be(request.Requirements);
        created.Deadline.Should().Be(request.Deadline);
        created.MaximumAvailableSlots.Should().Be(request.MaximumAvailableSlots);
        created.IsClosed.Should().BeFalse();
        created.Version.Should().Be(0);
        response.Headers.Location!.ToString()
            .Should().Be($"/api/v1/offers/{created.Id}");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking()
            .SingleOrDefaultAsync(offer => offer.Id == created.Id);
        persisted.Should().NotBeNull();
        persisted!.Title.Should().Be(request.Title);
        persisted.CompanyName.Should().Be(request.CompanyName);
        persisted.Requirements.Should().Be(request.Requirements);
        persisted.Deadline.Should().Be(request.Deadline);
        persisted.MaximumAvailableSlots.Should().Be(request.MaximumAvailableSlots);
        persisted.IsClosed.Should().BeFalse();
        persisted.Version.Should().Be(0);
    }

    [Fact]
    public async Task Given_InvalidOfferData_When_CreatingOffer_Then_ReturnsBadRequestAndPersistsNothing()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var request = new CreateOfferRequest(
            "",
            "Example Company",
            "C#, Git",
            BackendDeadline,
            2);

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/offers", request);
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        error.Should().Contain("Title is required.");

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ExistingOffer_When_GettingOfferById_Then_ReturnsOkAndOfferData()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var request = new CreateOfferRequest(
            "Backend Intern",
            "Example Company",
            "C#, Git",
            BackendDeadline,
            2);
        var created = await CreateOffer(client, request);

        // Act
        using var response = await client.GetAsync($"/api/v1/offers/{created.Id}");
        var returned = await response.Content.ReadFromJsonAsync<OfferResponse>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().BeEquivalentTo(created);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().AnyAsync(offer => offer.Id == created.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Given_MissingOffer_When_GettingOfferById_Then_ReturnsNotFound()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var missingOfferId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/offers/{missingOfferId}");
        var error = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        error.Should().Contain(missingOfferId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_MultipleOffers_When_GettingOffers_Then_ReturnsAllPersistedOffers()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var first = await CreateOffer(client, new CreateOfferRequest(
            "Backend Intern",
            "Example Company",
            "C#, Git",
            BackendDeadline,
            2));
        var second = await CreateOffer(client, new CreateOfferRequest(
            "Frontend Intern",
            "Example Company",
            "TypeScript, CSS",
            FrontendDeadline,
            1));

        // Act
        using var response = await client.GetAsync("/api/v1/offers");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().NotBeNull();
        returned.Should().BeEquivalentTo(new[] { first, second });

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(2);
        (await db.Offers.AsNoTracking().AnyAsync(offer => offer.Id == first.Id))
            .Should().BeTrue();
        (await db.Offers.AsNoTracking().AnyAsync(offer => offer.Id == second.Id))
            .Should().BeTrue();
    }

    [Fact]
    public async Task Given_NoOffers_When_GettingOffers_Then_ReturnsOkAndEmptyCollection()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();

        // Act
        using var response = await client.GetAsync("/api/v1/offers");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().NotBeNull();
        returned.Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static async Task<OfferResponse> CreateOffer(
        HttpClient client,
        CreateOfferRequest request)
    {
        using var response = await client.PostAsJsonAsync("/api/v1/offers", request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OfferResponse>();
        created.Should().NotBeNull();
        return created!;
    }

    private sealed record CreateOfferRequest(
        string Title,
        string CompanyName,
        string Requirements,
        DateTime Deadline,
        int MaximumAvailableSlots);

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
