using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.GetOfferCandidacies;

[Collection("Integration")]
public sealed class GetOfferCandidaciesEndpointTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_OfferWithCandidacies_When_Getting_Then_ReturnsOnlyThatOfferNewestFirstWithStudentNames()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var olderStudent = CreateStudent("Older Student");
        var newerStudent = CreateStudent("Newer Student");
        var offer = CreateOffer();
        var otherOffer = CreateOffer();
        var older = CreateCandidacy(olderStudent.Id, offer.Id, Now.AddDays(-1));
        var newer = CreateCandidacy(newerStudent.Id, offer.Id, Now);
        var otherOfferCandidacy = CreateCandidacy(olderStudent.Id, otherOffer.Id, Now.AddDays(1));
        await SeedAsync(
            factory,
            [olderStudent, newerStudent],
            [offer, otherOffer],
            [older, otherOfferCandidacy, newer]);

        // Act
        using var response = await client.GetAsync($"/api/v1/offers/{offer.Id}/candidacies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<GetOfferCandidaciesResponse[]>();
        body.Should().Equal(
            new GetOfferCandidaciesResponse(
                newer.Id,
                newerStudent.Id,
                "Newer Student",
                Now,
                InternshipCandidacyStatus.Submitted),
            new GetOfferCandidaciesResponse(
                older.Id,
                olderStudent.Id,
                "Older Student",
                Now.AddDays(-1),
                InternshipCandidacyStatus.Submitted));

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(3);
    }

    [Fact]
    public async Task Given_OfferWithoutCandidacies_When_Getting_Then_ReturnsEmptyList()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offer = CreateOffer();
        await SeedAsync(factory, [], [offer], []);

        // Act
        using var response = await client.GetAsync($"/api/v1/offers/{offer.Id}/candidacies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<GetOfferCandidaciesResponse[]>())
            .Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MissingOffer_When_Getting_Then_ReturnsNotFoundAndCreatesNoRows()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var offerId = Guid.NewGuid();

        // Act
        using var response = await client.GetAsync($"/api/v1/offers/{offerId}/candidacies");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"Offer {offerId} not found.");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.Students.AsNoTracking().CountAsync()).Should().Be(0);
        (await verificationDb.Offers.AsNoTracking().CountAsync()).Should().Be(0);
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    private static Student CreateStudent(string fullName) =>
        new(Guid.NewGuid(), fullName, $"student-{Guid.NewGuid():N}@example.test");

    private static Offer CreateOffer() =>
        new("Offer", "Company", ".NET", Now.AddDays(10), 1);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        DateTime applicationDate) =>
        new(
            Guid.NewGuid(),
            studentId,
            offerId,
            applicationDate,
            InternshipCandidacyStatus.Submitted);

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        IReadOnlyCollection<Student> students,
        IReadOnlyCollection<Offer> offers,
        IReadOnlyCollection<InternshipCandidacy> candidacies)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Students.AddRange(students);
        db.Offers.AddRange(offers);
        db.InternshipCandidacies.AddRange(candidacies);

        await db.SaveChangesAsync();
    }
}
