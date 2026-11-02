using FluentAssertions;
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

    [Fact]
    public void Given_SubmittedCandidacy_When_Shortlisting_Then_StatusBecomesShortlisted()
    {
        // Arrange
        var candidacy = CreateCandidacy(InternshipCandidacyStatus.Submitted);

        // Act
        var changed = candidacy.Shortlist();

        // Assert
        changed.Should().BeTrue();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
    }

    [Fact]
    public void Given_ShortlistedCandidacy_When_Shortlisting_Then_TransitionIsRefusedAndStatusUnchanged()
    {
        // Arrange
        var candidacy = CreateCandidacy(InternshipCandidacyStatus.Shortlisted);

        // Act
        var changed = candidacy.Shortlist();

        // Assert
        changed.Should().BeFalse();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public void Given_DecidableCandidacyAndAvailableSlot_When_Accepting_Then_StatusBecomesAccepted(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(initialStatus);

        // Act
        var changed = candidacy.Accept(hasAvailableSlot: true);

        // Assert
        changed.Should().BeTrue();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Accepted);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public void Given_DecidableCandidacyAndNoAvailableSlot_When_Accepting_Then_StatusBecomesWaitlisted(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(initialStatus);

        // Act
        var changed = candidacy.Accept(hasAvailableSlot: false);

        // Assert
        changed.Should().BeTrue();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Waitlisted);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public void Given_DecidableCandidacy_When_Rejecting_Then_StatusBecomesRejected(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(initialStatus);

        // Act
        var changed = candidacy.Reject();

        // Assert
        changed.Should().BeTrue();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Rejected);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public void Given_DecidableCandidacy_When_Withdrawing_Then_StatusBecomesWithdrawn(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(initialStatus);

        // Act
        var changed = candidacy.Withdraw();

        // Assert
        changed.Should().BeTrue();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public void Given_FinalCandidacy_When_Shortlisting_Then_TransitionIsRefusedAndStatusUnchanged(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(finalStatus);

        // Act
        var changed = candidacy.Shortlist();

        // Assert
        changed.Should().BeFalse();
        candidacy.Status.Should().Be(finalStatus);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted, true)]
    [InlineData(InternshipCandidacyStatus.Accepted, false)]
    [InlineData(InternshipCandidacyStatus.Rejected, true)]
    [InlineData(InternshipCandidacyStatus.Rejected, false)]
    [InlineData(InternshipCandidacyStatus.Waitlisted, true)]
    [InlineData(InternshipCandidacyStatus.Waitlisted, false)]
    [InlineData(InternshipCandidacyStatus.Withdrawn, true)]
    [InlineData(InternshipCandidacyStatus.Withdrawn, false)]
    public void Given_FinalCandidacy_When_Accepting_Then_TransitionIsRefusedAndStatusUnchanged(
        InternshipCandidacyStatus finalStatus,
        bool hasAvailableSlot)
    {
        // Arrange
        var candidacy = CreateCandidacy(finalStatus);

        // Act
        var changed = candidacy.Accept(hasAvailableSlot);

        // Assert
        changed.Should().BeFalse();
        candidacy.Status.Should().Be(finalStatus);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public void Given_FinalCandidacy_When_Rejecting_Then_TransitionIsRefusedAndStatusUnchanged(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(finalStatus);

        // Act
        var changed = candidacy.Reject();

        // Assert
        changed.Should().BeFalse();
        candidacy.Status.Should().Be(finalStatus);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public void Given_FinalCandidacy_When_Withdrawing_Then_TransitionIsRefusedAndStatusUnchanged(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(finalStatus);

        // Act
        var changed = candidacy.Withdraw();

        // Assert
        changed.Should().BeFalse();
        candidacy.Status.Should().Be(finalStatus);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted, true)]
    [InlineData(InternshipCandidacyStatus.Shortlisted, true)]
    [InlineData(InternshipCandidacyStatus.Accepted, true)]
    [InlineData(InternshipCandidacyStatus.Waitlisted, true)]
    [InlineData(InternshipCandidacyStatus.Rejected, false)]
    [InlineData(InternshipCandidacyStatus.Withdrawn, false)]
    public void Given_CandidacyWithStatus_When_ReadingIsActive_Then_ReflectsStatus(
        InternshipCandidacyStatus status,
        bool expectedIsActive)
    {
        // Arrange
        var candidacy = CreateCandidacy(status);

        // Act
        var isActive = candidacy.IsActive;

        // Assert
        isActive.Should().Be(expectedIsActive);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted, true)]
    [InlineData(InternshipCandidacyStatus.Shortlisted, true)]
    [InlineData(InternshipCandidacyStatus.Accepted, true)]
    [InlineData(InternshipCandidacyStatus.Waitlisted, true)]
    [InlineData(InternshipCandidacyStatus.Rejected, false)]
    [InlineData(InternshipCandidacyStatus.Withdrawn, false)]
    public void Given_Status_When_CheckingIsActiveStatus_Then_ReturnsWhetherStatusIsActive(
        InternshipCandidacyStatus status,
        bool expectedIsActive)
    {
        // Arrange
        var statusToCheck = status;

        // Act
        var isActive = InternshipCandidacy.IsActiveStatus(statusToCheck);

        // Assert
        isActive.Should().Be(expectedIsActive);
    }

    private static InternshipCandidacy CreateCandidacy(InternshipCandidacyStatus status) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateTime(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc),
            status);
}