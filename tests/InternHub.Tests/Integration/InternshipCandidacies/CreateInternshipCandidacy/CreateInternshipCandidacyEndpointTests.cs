using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.CreateInternshipCandidacy;

[Collection("Integration")]
public sealed class CreateInternshipCandidacyEndpointTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_EligibleStudentAndOpenOffer_When_Applying_Then_ReturnsCreatedAndPersistsSubmittedCandidacy()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(Now.AddDays(1));
        await SeedAsync(factory, [student], [offer]);
        var request = new CreateInternshipCandidacyRequest(student.Id, offer.Id);

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/candidacies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var body = await response.Content.ReadFromJsonAsync<CreateInternshipCandidacyResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().NotBeEmpty();
        body.StudentId.Should().Be(student.Id);
        body.OfferId.Should().Be(offer.Id);
        body.ApplicationDate.Should().Be(Now);
        body.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Be($"/api/v1/candidacies/{body.Id}");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var persisted = await verificationDb.InternshipCandidacies.AsNoTracking()
            .SingleAsync(item => item.Id == body.Id);
        persisted.StudentId.Should().Be(student.Id);
        persisted.OfferId.Should().Be(offer.Id);
        persisted.ApplicationDate.Should().Be(Now);
        persisted.Status.Should().Be(InternshipCandidacyStatus.Submitted);
    }

    [Fact]
    public async Task Given_EmptyStudentId_When_Applying_Then_ReturnsBadRequestWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var offer = CreateOffer(Now.AddDays(1));
        await SeedAsync(factory, [], [offer]);
        var request = new CreateInternshipCandidacyRequest(Guid.Empty, offer.Id);

        // Act
        using var response = await client.PostAsJsonAsync("/api/v1/candidacies", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).Should().Contain("must not be empty");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_MissingStudent_When_Applying_Then_ReturnsNotFoundWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var offer = CreateOffer(Now.AddDays(1));
        await SeedAsync(factory, [], [offer]);
        var studentId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(studentId, offer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"Student {studentId} not found.");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_MissingOffer_When_Applying_Then_ReturnsNotFoundWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        await SeedAsync(factory, [student], []);
        var offerId = Guid.NewGuid();

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, offerId));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"Offer {offerId} not found.");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ClosedOffer_When_Applying_Then_ReturnsConflictWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(Now.AddDays(1));
        offer.Close();
        await SeedAsync(factory, [student], [offer]);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, offer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("is closed");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ExpiredOffer_When_Applying_Then_ReturnsConflictWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(Now.AddTicks(-1));
        await SeedAsync(factory, [student], [offer]);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, offer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("application deadline");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_ActiveCandidacyForSameOffer_When_ApplyingAgain_Then_ReturnsConflictAndDoesNotAddRows()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(Now.AddDays(1));
        var existing = CreateCandidacy(
            student.Id,
            offer.Id,
            Now.AddDays(-1),
            InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [student], [offer], [existing]);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, offer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("already has an active candidacy");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(1);
        (await verificationDb.InternshipCandidacies.AsNoTracking()
            .SingleAsync(item => item.Id == existing.Id)).Should().BeEquivalentTo(existing);
    }

    [Fact]
    public async Task Given_FiveActiveCandidacies_When_ApplyingToAnotherOffer_Then_ReturnsConflictAndDoesNotAddRows()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var activeOffers = Enumerable.Range(0, 5)
            .Select(_ => CreateOffer(Now.AddDays(1)))
            .ToArray();
        var requestedOffer = CreateOffer(Now.AddDays(1));
        var activeCandidacies = activeOffers
            .Select((offer, index) => CreateCandidacy(
                student.Id,
                offer.Id,
                Now.AddDays(-index - 1),
                InternshipCandidacyStatus.Submitted))
            .ToArray();
        await SeedAsync(factory, [student], activeOffers.Append(requestedOffer).ToArray(), activeCandidacies);

        // Act
        using var response = await client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, requestedOffer.Id));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync()).Should().Contain("maximum of 5 active candidacies");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(5);
        (await verificationDb.InternshipCandidacies.AsNoTracking()
            .Select(item => item.Id).ToArrayAsync())
            .Should().BeEquivalentTo(activeCandidacies.Select(item => item.Id));
    }

    [Fact]
    public async Task Given_FourActiveCandidaciesAndTwoConcurrentApplications_When_Applying_Then_OnlyOneReachesTheFiveActiveLimit()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(
            new FixedTimeProvider(Now),
            useSeparateConnections: true);
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var existingOffers = Enumerable.Range(0, 4)
            .Select(_ => CreateOffer(Now.AddDays(1)))
            .ToArray();
        var requestedOffers = Enumerable.Range(0, 2)
            .Select(_ => CreateOffer(Now.AddDays(1)))
            .ToArray();
        var existingCandidacies = existingOffers
            .Select((offer, index) => CreateCandidacy(
                student.Id,
                offer.Id,
                Now.AddDays(-index - 1),
                InternshipCandidacyStatus.Submitted))
            .ToArray();
        await SeedAsync(
            factory,
            [student],
            [.. existingOffers, .. requestedOffers],
            existingCandidacies);
        var firstRequest = client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, requestedOffers[0].Id));
        var secondRequest = client.PostAsJsonAsync(
            "/api/v1/candidacies",
            new CreateInternshipCandidacyRequest(student.Id, requestedOffers[1].Id));

        // Act
        var responses = await Task.WhenAll(firstRequest, secondRequest);
        using var firstResponse = responses[0];
        using var secondResponse = responses[1];

        // Assert
        responses.Select(response => response.StatusCode)
            .Should().BeEquivalentTo(
                [HttpStatusCode.Created, HttpStatusCode.Conflict]);
        var createdResponse = responses.Single(
            response => response.StatusCode == HttpStatusCode.Created);
        var conflictResponse = responses.Single(
            response => response.StatusCode == HttpStatusCode.Conflict);
        (await conflictResponse.Content.ReadAsStringAsync())
            .Should().Contain("maximum of 5 active candidacies");
        var created = await createdResponse.Content
            .ReadFromJsonAsync<CreateInternshipCandidacyResponse>();
        created.Should().NotBeNull();
        created!.StudentId.Should().Be(student.Id);
        requestedOffers.Select(offer => offer.Id).Should().Contain(created.OfferId);
        created.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        createdResponse.Headers.Location.Should().NotBeNull();
        createdResponse.Headers.Location!.ToString()
            .Should().Be($"/api/v1/candidacies/{created.Id}");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb =
            verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var studentCandidacies = await verificationDb.InternshipCandidacies
            .AsNoTracking()
            .Where(item => item.StudentId == student.Id)
            .ToArrayAsync();
        var activeCandidacies = studentCandidacies
            .Where(item => item.IsActive)
            .ToArray();
        activeCandidacies.Should().HaveCount(5);
        activeCandidacies.Select(item => item.Id)
            .Should().Contain(existingCandidacies.Select(item => item.Id));
        activeCandidacies.Where(item =>
                requestedOffers.Select(offer => offer.Id).Contains(item.OfferId))
            .Should().ContainSingle()
            .Which.Id.Should().Be(created.Id);
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");

    private static Offer CreateOffer(DateTime deadline) =>
        new("Software Intern", "Example Company", ".NET", deadline, 1);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        DateTime applicationDate,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, applicationDate, status);

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        IReadOnlyCollection<Student> students,
        IReadOnlyCollection<Offer> offers,
        IReadOnlyCollection<InternshipCandidacy>? candidacies = null)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Students.AddRange(students);
        db.Offers.AddRange(offers);

        if (candidacies is not null)
            db.InternshipCandidacies.AddRange(candidacies);

        await db.SaveChangesAsync();
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
