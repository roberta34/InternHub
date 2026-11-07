using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Tests.Integration.Persistence;

public sealed class InternshipCandidacyConfigurationTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ActiveCandidacy_When_AnotherActiveCandidacyUsesSameStudentAndOffer_Then_SqliteRejectsIt()
    {
        // Arrange
        using var database = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var first = CreateCandidacy(
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted);
        var duplicate = CreateCandidacy(
            studentId,
            offerId,
            InternshipCandidacyStatus.Accepted);

        using var context = new InternHubDbContext(options);
        context.InternshipCandidacies.Add(first);
        context.SaveChanges();
        context.InternshipCandidacies.Add(duplicate);

        // Act
        var action = () => context.SaveChanges();

        // Assert
        action.Should().Throw<DbUpdateException>();

        using var verificationContext = new InternHubDbContext(options);
        verificationContext.InternshipCandidacies
            .AsNoTracking()
            .Should().ContainSingle(candidacy => candidacy.Id == first.Id);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public void Given_InactiveHistoricalCandidacy_When_AddingActiveCandidacyForSameStudentAndOffer_Then_AllowsIt(
        InternshipCandidacyStatus historicalStatus)
    {
        // Arrange
        using var database = CreateDatabase(out var options);
        var (studentId, offerId) = SeedStudentAndOffer(options);
        var historical = CreateCandidacy(studentId, offerId, historicalStatus);
        var active = CreateCandidacy(
            studentId,
            offerId,
            InternshipCandidacyStatus.Submitted);

        using var context = new InternHubDbContext(options);
        context.InternshipCandidacies.Add(historical);
        context.SaveChanges();
        context.InternshipCandidacies.Add(active);

        // Act
        var action = () => context.SaveChanges();

        // Assert
        action.Should().NotThrow();

        using var verificationContext = new InternHubDbContext(options);
        verificationContext.InternshipCandidacies
            .AsNoTracking()
            .Should().HaveCount(2);
    }

    [Fact]
    public void Given_SameOffer_When_DifferentStudentsSubmitActiveCandidacies_Then_AllowsBoth()
    {
        // Arrange
        using var database = CreateDatabase(out var options);
        var firstStudentId = SeedStudent(options);
        var secondStudentId = SeedStudent(options);
        var offerId = SeedOffer(options);
        var first = CreateCandidacy(
            firstStudentId,
            offerId,
            InternshipCandidacyStatus.Submitted);
        var second = CreateCandidacy(
            secondStudentId,
            offerId,
            InternshipCandidacyStatus.Submitted);

        using var context = new InternHubDbContext(options);
        context.InternshipCandidacies.AddRange(first, second);

        // Act
        var action = () => context.SaveChanges();

        // Assert
        action.Should().NotThrow();
        context.InternshipCandidacies.Should().HaveCount(2);
    }

    [Fact]
    public void Given_SameStudent_When_SubmittingToDifferentOffers_Then_AllowsBothActiveCandidacies()
    {
        // Arrange
        using var database = CreateDatabase(out var options);
        var studentId = SeedStudent(options);
        var firstOfferId = SeedOffer(options);
        var secondOfferId = SeedOffer(options);
        var first = CreateCandidacy(
            studentId,
            firstOfferId,
            InternshipCandidacyStatus.Submitted);
        var second = CreateCandidacy(
            studentId,
            secondOfferId,
            InternshipCandidacyStatus.Submitted);

        using var context = new InternHubDbContext(options);
        context.InternshipCandidacies.AddRange(first, second);

        // Act
        var action = () => context.SaveChanges();

        // Assert
        action.Should().NotThrow();
        context.InternshipCandidacies.Should().HaveCount(2);
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
        DbContextOptions<InternHubDbContext> options) =>
        (SeedStudent(options), SeedOffer(options));

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

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        Guid offerId,
        InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), studentId, offerId, ApplicationDate, status);
}
