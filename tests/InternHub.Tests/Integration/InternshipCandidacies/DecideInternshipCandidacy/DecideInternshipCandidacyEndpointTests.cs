using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.InternshipCandidacies.DecideInternshipCandidacy;

[Collection("Integration")]
public sealed class DecideInternshipCandidacyEndpointTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_SubmittedCandidacy_When_Shortlisting_Then_ReturnsOkAndPersistsShortlisted()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await PostDecisionAsync(client, candidacy.Id, "Shortlist");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<DecideInternshipCandidacyResponse>())
            .Should().Be(new DecideInternshipCandidacyResponse(
                candidacy.Id,
                InternshipCandidacyStatus.Shortlisted));
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Shortlisted);
    }

    [Fact]
    public async Task Given_SubmittedCandidacyAndFreeSlot_When_Accepting_Then_ReturnsOkAndPersistsAccepted()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await PostDecisionAsync(client, candidacy.Id, "Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<DecideInternshipCandidacyResponse>())
            .Should().Be(new DecideInternshipCandidacyResponse(
                candidacy.Id,
                InternshipCandidacyStatus.Accepted));
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Accepted);
    }

    [Fact]
    public async Task Given_AcceptedCountReachedMaximumSlots_When_Accepting_Then_ReturnsOkAndPersistsWaitlisted()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var acceptedStudent = CreateStudent();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var accepted = CreateCandidacy(acceptedStudent.Id, offer.Id, InternshipCandidacyStatus.Accepted);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [acceptedStudent, student], [offer], [accepted, candidacy]);

        // Act
        using var response = await PostDecisionAsync(client, candidacy.Id, "Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<DecideInternshipCandidacyResponse>())
            .Should().Be(new DecideInternshipCandidacyResponse(
                candidacy.Id,
                InternshipCandidacyStatus.Waitlisted));
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Waitlisted);

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking()
            .CountAsync(item => item.OfferId == offer.Id && item.Status == InternshipCandidacyStatus.Accepted))
            .Should().Be(1);
    }

    [Fact]
    public async Task Given_ClosedOffer_When_Accepting_Then_ReturnsOkAndPersistsAccepted()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        offer.Close();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Shortlisted);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await PostDecisionAsync(client, candidacy.Id, "Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadFromJsonAsync<DecideInternshipCandidacyResponse>())
            .Should().Be(new DecideInternshipCandidacyResponse(
                candidacy.Id,
                InternshipCandidacyStatus.Accepted));
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Accepted);
    }

    [Fact]
    public async Task Given_NumericAction_When_Deciding_Then_ReturnsBadRequestAndPreservesStatus()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await PostDecisionAsync(client, candidacy.Id, "1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync())
            .Should().Contain("Action '1' is not valid.");
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Submitted);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Deciding_Then_ReturnsNotFoundAndCreatesNoCandidacy()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var candidacyId = Guid.NewGuid();

        // Act
        using var response = await PostDecisionAsync(client, candidacyId, "Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync())
            .Should().Contain($"Candidacy {candidacyId} not found.");

        using var verificationScope = factory.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await verificationDb.InternshipCandidacies.AsNoTracking().CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task Given_RejectedCandidacy_When_Accepting_Then_ReturnsConflictAndPreservesStatus()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        using var client = factory.CreateClient();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Rejected);
        await SeedAsync(factory, [student], [offer], [candidacy]);

        // Act
        using var response = await PostDecisionAsync(client, candidacy.Id, "Accept");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await response.Content.ReadAsStringAsync())
            .Should().Contain($"Candidacy {candidacy.Id} cannot be accepted from status Rejected.");
        (await ReadStatusAsync(factory, candidacy.Id)).Should().Be(InternshipCandidacyStatus.Rejected);
    }

    private static Task<HttpResponseMessage> PostDecisionAsync(
        HttpClient client,
        Guid candidacyId,
        string action) =>
        client.PostAsJsonAsync(
            $"/api/v1/candidacies/{candidacyId}/decision",
            new DecideInternshipCandidacyRequest(action));

    private static async Task<InternshipCandidacyStatus> ReadStatusAsync(
        InternHubSqliteFactory factory,
        Guid candidacyId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

        return await db.InternshipCandidacies.AsNoTracking()
            .Where(candidacy => candidacy.Id == candidacyId)
            .Select(candidacy => candidacy.Status)
            .SingleAsync();
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Integration Student", $"student-{Guid.NewGuid():N}@example.test");

    private static Offer CreateOffer(int maximumAvailableSlots) =>
        new("Offer", "Company", ".NET", ApplicationDate.AddDays(10), maximumAvailableSlots);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, ApplicationDate, status);

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
