using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Domain.Students;

namespace InternHub.Tests.Unit.InternshipCandidacies;
public class InternshipCandidacyTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesInternshipCandidacy()
    {
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var offerId = Guid.NewGuid();
        var applicationDate = DateTime.UtcNow;

        var candidacy = new InternshipCandidacy(
            id,
            studentId,
            offerId,
            applicationDate,
            InternshipCandidacyStatus.Submitted);

        Assert.Equal(id, candidacy.Id);
        Assert.Equal(studentId, candidacy.StudentId);
        Assert.Equal(offerId, candidacy.OfferId);
        Assert.Equal(applicationDate, candidacy.ApplicationDate);
        Assert.Equal(
            InternshipCandidacyStatus.Submitted,
            candidacy.Status);
    }

    [Fact]
    public void Constructor_WithEmptyId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new InternshipCandidacy(
                Guid.Empty,
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTime.UtcNow,
                InternshipCandidacyStatus.Submitted));
    }

    [Fact]
    public void Constructor_WithEmptyStudentId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new InternshipCandidacy(
                Guid.NewGuid(),
                Guid.Empty,
                Guid.NewGuid(),
                DateTime.UtcNow,
                InternshipCandidacyStatus.Submitted));
    }

    [Fact]
    public void Constructor_WithEmptyOfferId_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new InternshipCandidacy(
                Guid.NewGuid(),
                Guid.NewGuid(),
                Guid.Empty,
                DateTime.UtcNow,
                InternshipCandidacyStatus.Submitted));
    }
    
    [Fact]
    public void Candidacy_ConnectsExistingStudentAndOfferById()
    {
        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        var offer = new Offer(
            "Software Engineer Intern",
            "Microsoft",
            "C#, .NET",
            DateTime.UtcNow.AddDays(30),
            3);

        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            student.Id,
            offer.Id,
            DateTime.UtcNow,
            InternshipCandidacyStatus.Submitted);

        Assert.Equal(student.Id, candidacy.StudentId);
        Assert.Equal(offer.Id, candidacy.OfferId);
        Assert.Equal(
            InternshipCandidacyStatus.Submitted,
            candidacy.Status);
    }
}