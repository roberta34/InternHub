using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicDecision;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Tests.Integration.Persistence.AtomicDecision;

public sealed class EfCoreAtomicCandidacyDecisionTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_SubmittedCandidacyAndFreeSlot_When_Accepting_Then_AcceptsAndIncrementsOfferVersion()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(database, [student], [offer], [candidacy]);
        var versionBefore = ReadOfferVersion(database, offer.Id);

        // Act
        var result = await DecideAsync(database, candidacy.Id, CandidacyDecision.Accept);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.Decided);
        result.Candidacy!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        ReadOfferVersion(database, offer.Id).Should().Be(versionBefore + 1);
    }

    [Fact]
    public async Task Given_SubmittedCandidacyAndNoFreeSlot_When_Accepting_Then_WaitlistsAndKeepsOfferVersion()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var acceptedStudent = CreateStudent();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var accepted = CreateCandidacy(acceptedStudent.Id, offer.Id, InternshipCandidacyStatus.Accepted);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(database, [acceptedStudent, student], [offer], [accepted, candidacy]);
        var versionBefore = ReadOfferVersion(database, offer.Id);

        // Act
        var result = await DecideAsync(database, candidacy.Id, CandidacyDecision.Accept);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.Decided);
        result.Candidacy!.Status.Should().Be(InternshipCandidacyStatus.Waitlisted);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Waitlisted);
        CountAccepted(database, offer.Id).Should().Be(1);
        ReadOfferVersion(database, offer.Id).Should().Be(versionBefore);
    }

    [Theory]
    [InlineData(CandidacyDecision.Shortlist, InternshipCandidacyStatus.Shortlisted)]
    [InlineData(CandidacyDecision.Reject, InternshipCandidacyStatus.Rejected)]
    public async Task Given_SubmittedCandidacy_When_ShortlistingOrRejecting_Then_DecidesAndKeepsOfferVersion(
        CandidacyDecision decision,
        InternshipCandidacyStatus expectedStatus)
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(database, [student], [offer], [candidacy]);
        var versionBefore = ReadOfferVersion(database, offer.Id);

        // Act
        var result = await DecideAsync(database, candidacy.Id, decision);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.Decided);
        result.Candidacy!.Status.Should().Be(expectedStatus);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(expectedStatus);
        ReadOfferVersion(database, offer.Id).Should().Be(versionBefore);
    }

    [Fact]
    public async Task Given_RejectedCandidacy_When_Accepting_Then_ReturnsInvalidTransitionAndChangesNothing()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Rejected);
        Seed(database, [student], [offer], [candidacy]);
        var versionBefore = ReadOfferVersion(database, offer.Id);

        // Act
        var result = await DecideAsync(database, candidacy.Id, CandidacyDecision.Accept);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.InvalidTransition);
        result.Candidacy.Should().BeNull();
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Rejected);
        ReadOfferVersion(database, offer.Id).Should().Be(versionBefore);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Deciding_Then_ReturnsCandidacyNotFoundAndChangesNothing()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var existing = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(database, [student], [offer], [existing]);
        var versionBefore = ReadOfferVersion(database, offer.Id);
        var missingCandidacyId = Guid.NewGuid();

        // Act
        var result = await DecideAsync(database, missingCandidacyId, CandidacyDecision.Accept);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.CandidacyNotFound);
        result.Candidacy.Should().BeNull();
        ReadCandidacy(database, missingCandidacyId).Should().BeNull();
        ReadCandidacy(database, existing.Id)!.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        ReadOfferVersion(database, offer.Id).Should().Be(versionBefore);
    }

    [Fact]
    public async Task Given_ClosedOfferWithFreeSlot_When_Accepting_Then_Accepts()
    {
        // Arrange
        using var database = new InMemoryDatabase();
        var student = CreateStudent();
        var offer = CreateOffer(maximumAvailableSlots: 1);
        offer.Close();
        var candidacy = CreateCandidacy(student.Id, offer.Id, InternshipCandidacyStatus.Submitted);
        Seed(database, [student], [offer], [candidacy]);

        // Act
        var result = await DecideAsync(database, candidacy.Id, CandidacyDecision.Accept);

        // Assert
        result.Status.Should().Be(AtomicCandidacyDecisionStatus.Decided);
        result.Candidacy!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        ReadCandidacy(database, candidacy.Id)!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
    }

    private static async Task<AtomicCandidacyDecisionResult> DecideAsync(
        InMemoryDatabase database,
        Guid candidacyId,
        CandidacyDecision decision)
    {
        await using var context = database.CreateContext();
        var decisions = new EfCoreAtomicCandidacyDecision(context);
        return await decisions.DecideAsync(candidacyId, decision);
    }

    private static void Seed(
        InMemoryDatabase database,
        Student[] students,
        Offer[] offers,
        InternshipCandidacy[] candidacies)
    {
        using var context = database.CreateContext();
        context.Students.AddRange(students);
        context.Offers.AddRange(offers);
        context.InternshipCandidacies.AddRange(candidacies);
        context.SaveChanges();
    }

    private static InternshipCandidacy? ReadCandidacy(InMemoryDatabase database, Guid candidacyId)
    {
        using var context = database.CreateContext();
        return context.InternshipCandidacies
            .AsNoTracking()
            .SingleOrDefault(candidacy => candidacy.Id == candidacyId);
    }

    private static int ReadOfferVersion(InMemoryDatabase database, Guid offerId)
    {
        using var context = database.CreateContext();
        return context.Offers
            .AsNoTracking()
            .Single(offer => offer.Id == offerId)
            .Version;
    }

    private static int CountAccepted(InMemoryDatabase database, Guid offerId)
    {
        using var context = database.CreateContext();
        return context.InternshipCandidacies
            .AsNoTracking()
            .Count(candidacy =>
                candidacy.OfferId == offerId &&
                candidacy.Status == InternshipCandidacyStatus.Accepted);
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Decision Student", $"student-{Guid.NewGuid()}@example.com");

    private static Offer CreateOffer(int maximumAvailableSlots) =>
        new("Decision Internship", "Example Company", "C#, SQL", Now.AddDays(10), maximumAvailableSlots);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, Now.AddHours(-1), status);

    private sealed class InMemoryDatabase : IDisposable
    {
        private readonly SqliteConnection _connection = new("Data Source=:memory:");

        public InMemoryDatabase()
        {
            _connection.Open();
            using var context = CreateContext();
            context.Database.EnsureCreated();
        }

        public InternHubDbContext CreateContext() =>
            new(new DbContextOptionsBuilder<InternHubDbContext>()
                .UseSqlite(_connection)
                .Options);

        public void Dispose() => _connection.Dispose();
    }
}
