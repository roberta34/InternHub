using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Offers.GetOffers;

[Collection("Integration")]
public sealed class GetOffersEndpointTests
{
    private static readonly DateTime CurrentUtc =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_OpenOfferWithFutureDeadline_When_GettingActiveOffers_Then_ReturnsOnlyThatOffer()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var activeOffer = CreateOffer("Active Intern", CurrentUtc.AddMinutes(1));
        var closedOffer = CreateOffer("Closed Intern", CurrentUtc.AddMinutes(1));
        closedOffer.Close();
        var expiredOffer = CreateOffer("Expired Intern", CurrentUtc.AddMinutes(-1));
        var equalDeadlineOffer = CreateOffer("Due Now Intern", CurrentUtc);
        await SeedOffers(factory, activeOffer, closedOffer, expiredOffer, equalDeadlineOffer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers?activeOnly=true");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().BeEquivalentTo(
            new[] { ToResponse(activeOffer) },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task Given_ClosedOffer_When_GettingActiveOffers_Then_ExcludesClosedOffer()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var closedOffer = CreateOffer("Closed Intern", CurrentUtc.AddMinutes(1));
        closedOffer.Close();
        await SeedOffers(factory, closedOffer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers?activeOnly=true");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().NotBeNull();
        returned.Should().BeEmpty();
        await AssertPersistedOffer(factory, closedOffer);
    }

    [Fact]
    public async Task Given_ExpiredOffer_When_GettingActiveOffers_Then_ExcludesExpiredOffer()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var expiredOffer = CreateOffer("Expired Intern", CurrentUtc.AddMinutes(-1));
        await SeedOffers(factory, expiredOffer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers?activeOnly=true");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().NotBeNull();
        returned.Should().BeEmpty();
        await AssertPersistedOffer(factory, expiredOffer);
    }

    [Fact]
    public async Task Given_OfferDueAtCurrentTime_When_GettingActiveOffers_Then_ExcludesOffer()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var offer = CreateOffer("Due Now Intern", CurrentUtc);
        await SeedOffers(factory, offer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers?activeOnly=true");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().NotBeNull();
        returned.Should().BeEmpty();
        await AssertPersistedOffer(factory, offer);
    }

    [Fact]
    public async Task Given_OpenClosedAndExpiredOffers_When_GettingAllOffersExplicitly_Then_ReturnsAllOffers()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var openOffer = CreateOffer("Open Intern", CurrentUtc.AddMinutes(1));
        var closedOffer = CreateOffer("Closed Intern", CurrentUtc.AddMinutes(1));
        closedOffer.Close();
        var expiredOffer = CreateOffer("Expired Intern", CurrentUtc.AddMinutes(-1));
        await SeedOffers(factory, openOffer, closedOffer, expiredOffer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers?activeOnly=false");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().BeEquivalentTo(
            new[]
            {
                ToResponse(openOffer),
                ToResponse(closedOffer),
                ToResponse(expiredOffer)
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Given_OpenClosedAndExpiredOffers_When_GettingOffersWithoutActiveOnlyParameter_Then_ReturnsAllOffers()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var openOffer = CreateOffer("Open Intern", CurrentUtc.AddMinutes(1));
        var closedOffer = CreateOffer("Closed Intern", CurrentUtc.AddMinutes(1));
        closedOffer.Close();
        var expiredOffer = CreateOffer("Expired Intern", CurrentUtc.AddMinutes(-1));
        await SeedOffers(factory, openOffer, closedOffer, expiredOffer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers");
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        returned.Should().BeEquivalentTo(
            new[]
            {
                ToResponse(openOffer),
                ToResponse(closedOffer),
                ToResponse(expiredOffer)
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task Given_NoActiveOffers_When_GettingActiveOffers_Then_ReturnsOkAndEmptyJsonArray()
    {
        // Arrange
        using var factory = CreateFactory();
        using var client = factory.CreateClient();
        var closedOffer = CreateOffer("Closed Intern", CurrentUtc.AddMinutes(1));
        closedOffer.Close();
        var expiredOffer = CreateOffer("Expired Intern", CurrentUtc.AddMinutes(-1));
        await SeedOffers(factory, closedOffer, expiredOffer);

        // Act
        using var response = await client.GetAsync("/api/v1/offers?activeOnly=true");
        var body = await response.Content.ReadAsStringAsync();
        var returned = await response.Content
            .ReadFromJsonAsync<IReadOnlyList<OfferResponse>>();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Be("[]");
        returned.Should().NotBeNull();
        returned.Should().BeEmpty();
    }

    private static InternHubSqliteFactory CreateFactory() =>
        new(new FixedTimeProvider(CurrentUtc));

    private static Offer CreateOffer(string title, DateTime deadline) =>
        new(title, "Example Company", "C#, Git", deadline, 2);

    private static async Task SeedOffers(
        InternHubSqliteFactory factory,
        params Offer[] offers)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Offers.AddRange(offers);
        await db.SaveChangesAsync();
    }

    private static async Task AssertPersistedOffer(
        InternHubSqliteFactory factory,
        Offer expected)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await db.Offers.AsNoTracking()
            .SingleAsync(offer => offer.Id == expected.Id);
        persisted.Should().BeEquivalentTo(expected);
    }

    private static OfferResponse ToResponse(Offer offer) =>
        new(
            offer.Id,
            offer.Title,
            offer.CompanyName,
            offer.Requirements,
            offer.Deadline,
            offer.MaximumAvailableSlots,
            offer.IsClosed,
            offer.Version);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
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
