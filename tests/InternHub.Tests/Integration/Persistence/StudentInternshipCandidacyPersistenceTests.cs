using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Persistence;
using InternHub.Domain.Students;
using InternHub.Tests.Integration.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Tests.Integration.Persistence;

public sealed class StudentInternshipCandidacyPersistenceTests
{
    private static readonly DateTime ApplicationDate =
        new(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ValidStudentOfferAndCandidacy_When_Persisting_Then_ReloadsAllRelationshipData()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        var studentId = Guid.NewGuid();
        var offer = new Offer(
            "Backend Intern",
            "Example Company",
            "C#, Git",
            Deadline,
            2);
        var candidacyId = Guid.NewGuid();
        var candidacy = new InternshipCandidacy(
            candidacyId,
            studentId,
            offer.Id,
            ApplicationDate,
            InternshipCandidacyStatus.Submitted);

        using (var scope = factory.Services.CreateScope())
        {
            var studentRepository = scope.ServiceProvider
                .GetRequiredService<IStudentRepository>();
            var offerRepository = scope.ServiceProvider
                .GetRequiredService<IOfferRepository>();
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

            studentRepository.Add(new Student(
                studentId,
                "Ada Lovelace",
                "ada@example.com")).Should().BeTrue();
            offerRepository.Add(offer).Should().BeTrue();
            db.InternshipCandidacies.Add(candidacy);
            db.SaveChanges();
        }

        // Act
        using var reloadScope = factory.Services.CreateScope();
        var reloadedStudentRepository = reloadScope.ServiceProvider
            .GetRequiredService<IStudentRepository>();
        var reloadedOfferRepository = reloadScope.ServiceProvider
            .GetRequiredService<IOfferRepository>();
        var reloadedDb = reloadScope.ServiceProvider
            .GetRequiredService<InternHubDbContext>();
        var reloadedStudent = reloadedStudentRepository.GetById(studentId);
        var reloadedOffer = reloadedOfferRepository.GetById(offer.Id);
        var reloadedCandidacy = reloadedDb.InternshipCandidacies
            .AsNoTracking()
            .Single(item => item.Id == candidacyId);

        // Assert
        reloadedStudent.Should().NotBeNull();
        reloadedStudent!.FullName.Should().Be("Ada Lovelace");
        reloadedStudent.Email.Should().Be("ada@example.com");
        reloadedStudent.InternshipCandidacies.Should().ContainSingle(item =>
            item.Id == candidacyId &&
            item.StudentId == studentId &&
            item.OfferId == offer.Id);
        reloadedOffer.Should().NotBeNull();
        reloadedOffer!.Title.Should().Be("Backend Intern");
        reloadedOffer.InternshipCandidacies.Should().ContainSingle(item =>
            item.Id == candidacyId &&
            item.StudentId == studentId &&
            item.OfferId == offer.Id);
        reloadedCandidacy.ApplicationDate.Should().Be(ApplicationDate);
        reloadedCandidacy.Status.Should().Be(InternshipCandidacyStatus.Submitted);
    }

    [Fact]
    public async Task Given_StudentWithCandidacy_When_DeletingStudent_Then_CandidacyIsCascadedAndOfferRemains()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        var studentId = Guid.NewGuid();
        var offer = new Offer(
            "Frontend Intern",
            "Example Company",
            "TypeScript, CSS",
            Deadline,
            1);
        var candidacyId = Guid.NewGuid();

        using (var scope = factory.Services.CreateScope())
        {
            var studentRepository = scope.ServiceProvider
                .GetRequiredService<IStudentRepository>();
            var offerRepository = scope.ServiceProvider
                .GetRequiredService<IOfferRepository>();
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();

            studentRepository.Add(new Student(
                studentId,
                "Grace Hopper",
                "grace@example.com")).Should().BeTrue();
            offerRepository.Add(offer).Should().BeTrue();
            db.InternshipCandidacies.Add(new InternshipCandidacy(
                candidacyId,
                studentId,
                offer.Id,
                ApplicationDate,
                InternshipCandidacyStatus.Shortlisted));
            db.SaveChanges();
        }

        // Act
        using (var deleteScope = factory.Services.CreateScope())
        {
            var studentRepository = deleteScope.ServiceProvider
                .GetRequiredService<IStudentRepository>();

            studentRepository.Delete(studentId).Should().BeTrue();
        }

        // Assert
        using var assertScope = factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider
            .GetRequiredService<InternHubDbContext>();
        (await assertDb.Students.AsNoTracking()
            .AnyAsync(item => item.Id == studentId))
            .Should().BeFalse();
        (await assertDb.InternshipCandidacies.AsNoTracking()
            .AnyAsync(item => item.Id == candidacyId))
            .Should().BeFalse();
        var remainingOffer = assertDb.Offers
            .Include(item => item.InternshipCandidacies)
            .AsNoTracking()
            .SingleOrDefault(item => item.Id == offer.Id);
        remainingOffer.Should().NotBeNull();
        remainingOffer!.InternshipCandidacies.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MissingStudentReference_When_PersistingCandidacy_Then_SqliteRejectsForeignKey()
    {
        // Arrange
        using var factory = new InternHubSqliteFactory();
        var missingStudentId = Guid.NewGuid();
        var offer = new Offer(
            "Data Intern",
            "Example Company",
            "SQL, Python",
            Deadline,
            1);
        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            missingStudentId,
            offer.Id,
            ApplicationDate,
            InternshipCandidacyStatus.Submitted);

        using (var setupScope = factory.Services.CreateScope())
        {
            var offerRepository = setupScope.ServiceProvider
                .GetRequiredService<IOfferRepository>();

            offerRepository.Add(offer).Should().BeTrue();
        }

        // Act
        DbUpdateException exception;
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<InternHubDbContext>();
            db.InternshipCandidacies.Add(candidacy);
            exception = FluentActions.Invoking(() => db.SaveChanges())
                .Should().Throw<DbUpdateException>().Which;
        }

        // Assert
        exception.InnerException.Should().BeOfType<SqliteException>()
            .Which.SqliteErrorCode.Should().Be(19);
        using var assertScope = factory.Services.CreateScope();
        var assertDb = assertScope.ServiceProvider
            .GetRequiredService<InternHubDbContext>();
        (await assertDb.InternshipCandidacies.AsNoTracking()
            .AnyAsync(item => item.Id == candidacy.Id))
            .Should().BeFalse();
    }
}
