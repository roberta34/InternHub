using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Api.Infrastructure.Persistence.AtomicSubmission;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Persistence.AtomicSubmission;

public sealed class EfCoreAtomicCandidacySubmissionTests
{
    private static readonly DateTime Now =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_EligibleStudentAndOffer_When_Submitting_Then_CreatesSubmittedCandidacy()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var student = new Student(
            Guid.NewGuid(),
            "Ada Lovelace",
            "ada.atomic@example.com");
        var offer = CreateOffer(Now.AddDays(1));
        await SeedAsync(factory, student, offer);

        AtomicCandidacySubmissionResult result;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
            var submission = new EfCoreAtomicCandidacySubmission(
                db,
                new FixedTimeProvider(Now));

            // Act
            result = await submission.SubmitAsync(student.Id, offer.Id);
        }

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.Created);
        result.Candidacy.Should().NotBeNull();
        result.Candidacy!.StudentId.Should().Be(student.Id);
        result.Candidacy.OfferId.Should().Be(offer.Id);
        result.Candidacy.ApplicationDate.Should().Be(Now);
        result.Candidacy.Status.Should().Be(InternshipCandidacyStatus.Submitted);

        using var assertScope = factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await assertDb.InternshipCandidacies.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Given_MissingStudent_When_Submitting_Then_ReturnsStudentNotFoundWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var offer = CreateOffer(Now.AddDays(1));
        await SeedAsync(factory, null, offer);

        // Act
        var result = await SubmitAsync(factory, Guid.NewGuid(), offer.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.StudentNotFound);
        await AssertNoCandidaciesAsync(factory);
    }

    [Fact]
    public async Task Given_MissingOffer_When_Submitting_Then_ReturnsOfferNotFoundWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var student = CreateStudent();
        await SeedAsync(factory, student, null);

        // Act
        var result = await SubmitAsync(factory, student.Id, Guid.NewGuid());

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.OfferNotFound);
        await AssertNoCandidaciesAsync(factory);
    }

    [Fact]
    public async Task Given_ClosedOffer_When_Submitting_Then_ReturnsOfferClosedWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var student = CreateStudent();
        var offer = CreateOffer(Now.AddDays(1));
        offer.Close();
        await SeedAsync(factory, student, offer);

        // Act
        var result = await SubmitAsync(factory, student.Id, offer.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.OfferClosed);
        await AssertNoCandidaciesAsync(factory);
    }

    [Fact]
    public async Task Given_ExpiredOffer_When_Submitting_Then_ReturnsDeadlineExpiredWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var student = CreateStudent();
        var offer = CreateOffer(Now);
        await SeedAsync(factory, student, offer);

        // Act
        var result = await SubmitAsync(factory, student.Id, offer.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.DeadlineExpired);
        await AssertNoCandidaciesAsync(factory);
    }

    [Fact]
    public async Task Given_DuplicateActiveCandidacy_When_Submitting_Then_ReturnsDuplicateActiveWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var student = CreateStudent();
        var offer = CreateOffer(Now.AddDays(1));
        var existing = CreateCandidacy(
            student.Id,
            offer.Id,
            InternshipCandidacyStatus.Waitlisted);
        await SeedAsync(factory, student, offer, existing);

        // Act
        var result = await SubmitAsync(factory, student.Id, offer.Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.DuplicateActive);
        await AssertCandidacyCountAsync(factory, 1);
    }

    [Fact]
    public async Task Given_FiveActiveCandidacies_When_Submitting_Then_ReturnsActiveLimitReachedWithoutPersisting()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory(new FixedTimeProvider(Now));
        var student = CreateStudent();
        var offers = Enumerable.Range(0, 6)
            .Select(_ => CreateOffer(Now.AddDays(1)))
            .ToArray();
        var existing = offers
            .Take(5)
            .Select((offer, index) => CreateCandidacy(
                student.Id,
                offer.Id,
                index == 0
                    ? InternshipCandidacyStatus.Submitted
                    : InternshipCandidacyStatus.Accepted))
            .ToArray();
        await SeedAsync(factory, student, offers, existing);

        // Act
        var result = await SubmitAsync(factory, student.Id, offers[5].Id);

        // Assert
        result.Status.Should().Be(AtomicCandidacySubmissionStatus.ActiveLimitReached);
        await AssertCandidacyCountAsync(factory, 5);
    }

    private static async Task<AtomicCandidacySubmissionResult> SubmitAsync(
        InternHubSqliteFactory factory,
        Guid studentId,
        Guid offerId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        var submission = new EfCoreAtomicCandidacySubmission(
            db,
            new FixedTimeProvider(Now));

        return await submission.SubmitAsync(studentId, offerId);
    }

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        Student? student,
        Offer? offer,
        params InternshipCandidacy[] candidacies)
    {
        await SeedAsync(
            factory,
            student is null ? [] : [student],
            offer is null ? [] : [offer],
            candidacies);
    }

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        Student student,
        Offer[] offers,
        InternshipCandidacy[] candidacies)
    {
        await SeedAsync(factory, [student], offers, candidacies);
    }

    private static async Task SeedAsync(
        InternHubSqliteFactory factory,
        Student[] students,
        Offer[] offers,
        InternshipCandidacy[] candidacies)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        db.Students.AddRange(students);
        db.Offers.AddRange(offers);
        db.InternshipCandidacies.AddRange(candidacies);
        await db.SaveChangesAsync();
    }

    private static async Task AssertNoCandidaciesAsync(InternHubSqliteFactory factory) =>
        await AssertCandidacyCountAsync(factory, 0);

    private static async Task AssertCandidacyCountAsync(
        InternHubSqliteFactory factory,
        int expected)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
        (await db.InternshipCandidacies.CountAsync()).Should().Be(expected);
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Test Student", $"student-{Guid.NewGuid()}@example.com");

    private static Offer CreateOffer(DateTime deadline) =>
        new("Test Internship", "Test Company", "C#, SQL", deadline, 2);

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, Now.AddHours(-1), status);

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
