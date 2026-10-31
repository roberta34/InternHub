using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Tests.Integration.Persistence;

public sealed class EfCoreInternshipCandidacyRepositoryTests
{
    private static readonly DateTime FirstApplicationDate =
        new(2027, 1, 10, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime SecondApplicationDate =
        new(2027, 1, 11, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ValidCandidacy_When_Adding_Then_PersistsAndReturnsTrue()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var candidacy = CreateCandidacy(
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted);

        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);

            // Act
            var added = repository.Add(candidacy);

            // Assert
            added.Should().BeTrue();
        }

        using var verificationContext = new InternHubDbContext(options);
        var persisted = verificationContext.InternshipCandidacies
            .AsNoTracking()
            .Single(item => item.Id == candidacy.Id);
        persisted.StudentId.Should().Be(studentId);
        persisted.OfferId.Should().Be(offerId);
        persisted.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        persisted.ApplicationDate.Should().Be(FirstApplicationDate);
    }

    [Fact]
    public void Given_ExistingCandidacy_When_GettingById_Then_ReturnsPersistedCandidacy()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var candidacy = SeedCandidacy(
            options,
            studentId,
            offerId,
            InternshipCandidacyStatus.Shortlisted);

        // Act
        InternshipCandidacy? result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.GetById(candidacy.Id);
        }

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(candidacy);
    }

    [Fact]
    public void Given_MissingCandidacyId_When_GettingById_Then_ReturnsNull()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var missingId = Guid.NewGuid();

        // Act
        InternshipCandidacy? result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.GetById(missingId);
        }

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void Given_CandidaciesForMultipleStudents_When_GettingByStudent_Then_ReturnsOnlyRequestedStudentCandidaciesInOrder()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var otherOfferId = SeedOffer(options);
        var otherStudentId = SeedStudent(options);
        var first = SeedCandidacy(
            options,
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted,
            FirstApplicationDate);
        var second = SeedCandidacy(
            options,
            studentId,
            otherOfferId,
            InternshipCandidacyStatus.Shortlisted,
            SecondApplicationDate);
        SeedCandidacy(
            options,
            otherStudentId,
            offerId,
            InternshipCandidacyStatus.Accepted,
            SecondApplicationDate);

        // Act
        IReadOnlyList<InternshipCandidacy> result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.GetByStudent(studentId);
        }

        // Assert
        result.Should().BeEquivalentTo(
            new[] { second, first },
            options => options.WithStrictOrdering());
    }

    [Fact]
    public void Given_CandidaciesForMultipleOffers_When_GettingByOffer_Then_ReturnsOnlyRequestedOfferCandidaciesInOrder()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var otherOfferId = SeedOffer(options);
        var otherStudentId = SeedStudent(options);
        var first = SeedCandidacy(
            options,
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted,
            FirstApplicationDate);
        var second = SeedCandidacy(
            options,
            otherStudentId,
            offerId,
            InternshipCandidacyStatus.Shortlisted,
            SecondApplicationDate);
        SeedCandidacy(
            options,
            otherStudentId,
            otherOfferId,
            InternshipCandidacyStatus.Accepted,
            SecondApplicationDate);

        // Act
        IReadOnlyList<InternshipCandidacy> result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.GetByOffer(offerId);
        }

        // Assert
        result.Should().BeEquivalentTo(
            new[] { second, first },
            options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted, true)]
    [InlineData(InternshipCandidacyStatus.Shortlisted, true)]
    [InlineData(InternshipCandidacyStatus.Accepted, true)]
    [InlineData(InternshipCandidacyStatus.Waitlisted, true)]
    [InlineData(InternshipCandidacyStatus.Rejected, false)]
    [InlineData(InternshipCandidacyStatus.Withdrawn, false)]
    public void Given_CandidacyWithStatus_When_CheckingActiveForStudentAndOffer_Then_ReturnsExpectedResult(
        InternshipCandidacyStatus status,
        bool expected)
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        SeedCandidacy(options, studentId, offerId, status);

        // Act
        bool result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.HasActiveForStudentAndOffer(studentId, offerId);
        }

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void Given_CandidaciesWithActiveAndInactiveStatuses_When_CountingActiveForStudent_Then_CountsOnlyActiveCandidacies()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var otherOfferId = SeedOffer(options);
        SeedCandidacy(options, studentId, offerId, InternshipCandidacyStatus.Submitted);
        SeedCandidacy(options, studentId, otherOfferId, InternshipCandidacyStatus.Waitlisted);
        SeedCandidacy(options, studentId, offerId, InternshipCandidacyStatus.Rejected);
        SeedCandidacy(options, studentId, offerId, InternshipCandidacyStatus.Withdrawn);

        // Act
        int result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.CountActiveForStudent(studentId);
        }

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void Given_CandidaciesWithAcceptedAndOtherStatuses_When_CountingAcceptedForOffer_Then_CountsOnlyAcceptedCandidacies()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var otherStudentId = SeedStudent(options);
        var thirdStudentId = SeedStudent(options);
        var otherOfferId = SeedOffer(options);
        SeedCandidacy(options, studentId, offerId, InternshipCandidacyStatus.Accepted);
        SeedCandidacy(options, otherStudentId, offerId, InternshipCandidacyStatus.Accepted);
        SeedCandidacy(options, thirdStudentId, offerId, InternshipCandidacyStatus.Shortlisted);
        SeedCandidacy(options, studentId, otherOfferId, InternshipCandidacyStatus.Accepted);

        // Act
        int result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.CountAcceptedForOffer(offerId);
        }

        // Assert
        result.Should().Be(2);
    }

    [Fact]
    public void Given_ActiveCandidacyForAnotherStudent_When_CheckingStudentAndOffer_Then_ReturnsFalse()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var otherStudentId = SeedStudent(options);
        SeedCandidacy(
            options,
            otherStudentId,
            offerId,
            InternshipCandidacyStatus.Submitted);

        // Act
        bool result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.HasActiveForStudentAndOffer(studentId, offerId);
        }

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Given_ActiveCandidacyForAnotherOffer_When_CheckingStudentAndOffer_Then_ReturnsFalse()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var otherOfferId = SeedOffer(options);
        SeedCandidacy(
            options,
            studentId,
            otherOfferId,
            InternshipCandidacyStatus.Submitted);

        // Act
        bool result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.HasActiveForStudentAndOffer(studentId, offerId);
        }

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Given_ExistingCandidacy_When_UpdatingStatus_Then_PersistsUpdatedStatus()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var original = SeedCandidacy(
            options,
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted);
        var updated = new InternshipCandidacy(
            original.Id,
            studentId,
            offerId,
            original.ApplicationDate,
            InternshipCandidacyStatus.Shortlisted);

        // Act
        bool result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.Update(updated);
        }

        // Assert
        result.Should().BeTrue();
        using var verificationContext = new InternHubDbContext(options);
        var persisted = verificationContext.InternshipCandidacies
            .AsNoTracking()
            .Single(item => item.Id == original.Id);
        persisted.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
    }

    [Fact]
    public void Given_MissingCandidacy_When_Updating_Then_ReturnsFalseAndPersistsNothing()
    {
        // Arrange
        using var connection = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var missing = CreateCandidacy(
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted);

        // Act
        bool result;
        using (var context = new InternHubDbContext(options))
        {
            var repository = new EfCoreInternshipCandidacyRepository(context);
            result = repository.Update(missing);
        }

        // Assert
        result.Should().BeFalse();
        using var verificationContext = new InternHubDbContext(options);
        (verificationContext.InternshipCandidacies.AsNoTracking()
            .Any(item => item.Id == missing.Id))
            .Should().BeFalse();
    }

    private static SqliteConnection CreateDatabase(
        out DbContextOptions<InternHubDbContext> options)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        options = new DbContextOptionsBuilder<InternHubDbContext>()
            .UseSqlite(connection)
            .Options;

        using var context = new InternHubDbContext(options);
        context.Database.EnsureCreated();
        return connection;
    }

    private static (Guid StudentId, Guid OfferId) SeedStudentAndOffer(
        DbContextOptions<InternHubDbContext> options)
    {
        var studentId = SeedStudent(options);
        var offerId = SeedOffer(options);
        return (studentId, offerId);
    }

    private static Guid SeedStudent(DbContextOptions<InternHubDbContext> options)
    {
        var student = new Student(
            Guid.NewGuid(),
            "Test Student",
            $"student-{Guid.NewGuid()}@example.com");

        using var context = new InternHubDbContext(options);
        context.Students.Add(student);
        context.SaveChanges();
        return student.Id;
    }

    private static Guid SeedOffer(DbContextOptions<InternHubDbContext> options)
    {
        var offer = new Offer(
            "Test Internship",
            "Test Company",
            "C#, SQL",
            new DateTime(2027, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            2);

        using var context = new InternHubDbContext(options);
        context.Offers.Add(offer);
        context.SaveChanges();
        return offer.Id;
    }

    private static InternshipCandidacy SeedCandidacy(
        DbContextOptions<InternHubDbContext> options,
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status,
        DateTime? applicationDate = null)
    {
        var candidacy = CreateCandidacy(
            studentId,
            offerId,
            status,
            applicationDate);

        using var context = new InternHubDbContext(options);
        context.InternshipCandidacies.Add(candidacy);
        context.SaveChanges();
        return candidacy;
    }

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status,
        DateTime? applicationDate = null) =>
        new(
            Guid.NewGuid(),
            studentId,
            offerId,
            applicationDate ?? FirstApplicationDate,
            status);
}
