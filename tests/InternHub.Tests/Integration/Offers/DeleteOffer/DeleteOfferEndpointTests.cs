using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Offers.DeleteOffer;

public sealed class DeleteOfferEndpointTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_ExistingOffer_When_DeletingOffer_Then_ReturnsNoContentAndRemovesOffer()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offerId = await CreateOffer(client);

        // Act
        using var response = await client.DeleteAsync($"/api/v1/offers/{offerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsStringAsync()).Should().BeEmpty();

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().AnyAsync(item => item.Id == offerId)).Should().BeFalse();
    }

    [Fact]
    public async Task Given_MissingOffer_When_DeletingOffer_Then_ReturnsNotFoundAndPreservesDatabase()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offerId = Guid.NewGuid();

        // Act
        using var response = await client.DeleteAsync($"/api/v1/offers/{offerId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain(offerId.ToString());

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.Offers.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static async Task<Guid> CreateOffer(HttpClient client)
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
        return created!.Id;
    }

    private sealed record OfferResponse(Guid Id);
}
