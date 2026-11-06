using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies.GetStudentCandidacySummary;

public sealed class GetStudentCandidacySummaryHandlerTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_MissingStudent_When_QueryingSummary_Then_ReturnsNotFoundWithoutDto()
    {
        // Arrange
        var students = new FakeStudentRepository();
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new GetStudentCandidacySummaryHandler(students, candidacies);

        // Act
        var result = handler.Handle(new GetStudentCandidacySummaryQuery(Guid.NewGuid()));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Given_ExistingStudentWithoutCandidacies_When_QueryingSummary_Then_ReturnsZeroCounts()
    {
        // Arrange
        var student = CreateStudent();
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            new FakeInternshipCandidacyRepository());

        // Act
        var result = handler.Handle(new GetStudentCandidacySummaryQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.StudentId.Should().Be(student.Id);
        result.Value.TotalCount.Should().Be(0);
        result.Value.ActiveCount.Should().Be(0);
        result.Value.SubmittedCount.Should().Be(0);
        result.Value.ShortlistedCount.Should().Be(0);
        result.Value.AcceptedCount.Should().Be(0);
        result.Value.RejectedCount.Should().Be(0);
        result.Value.WaitlistedCount.Should().Be(0);
        result.Value.WithdrawnCount.Should().Be(0);
    }

    [Fact]
    public void Given_StudentWithAllStatuses_When_QueryingSummary_Then_ReturnsCorrectCountsAndActivity()
    {
        // Arrange
        var student = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        var submitted = CreateCandidacy(student.Id, InternshipCandidacyStatus.Submitted);
        var shortlisted = CreateCandidacy(student.Id, InternshipCandidacyStatus.Shortlisted);
        var accepted = CreateCandidacy(student.Id, InternshipCandidacyStatus.Accepted);
        var rejected = CreateCandidacy(student.Id, InternshipCandidacyStatus.Rejected);
        var waitlisted = CreateCandidacy(student.Id, InternshipCandidacyStatus.Waitlisted);
        var withdrawn = CreateCandidacy(student.Id, InternshipCandidacyStatus.Withdrawn);
        candidacies.Seed(submitted);
        candidacies.Seed(shortlisted);
        candidacies.Seed(accepted);
        candidacies.Seed(rejected);
        candidacies.Seed(waitlisted);
        candidacies.Seed(withdrawn);

        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(students, candidacies);

        // Act
        var result = handler.Handle(new GetStudentCandidacySummaryQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(6);
        result.Value.ActiveCount.Should().Be(4);
        result.Value.SubmittedCount.Should().Be(1);
        result.Value.ShortlistedCount.Should().Be(1);
        result.Value.AcceptedCount.Should().Be(1);
        result.Value.RejectedCount.Should().Be(1);
        result.Value.WaitlistedCount.Should().Be(1);
        result.Value.WithdrawnCount.Should().Be(1);
        accepted.IsActive.Should().BeTrue();
        waitlisted.IsActive.Should().BeTrue();
        rejected.IsActive.Should().BeFalse();
        withdrawn.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Given_MultipleCandidaciesWithSameStatus_When_QueryingSummary_Then_AggregatesEachStatus()
    {
        // Arrange
        var student = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Submitted));
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Submitted));
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Accepted));
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Accepted));
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Rejected));

        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(students, candidacies);

        // Act
        var result = handler.Handle(new GetStudentCandidacySummaryQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(5);
        result.Value.ActiveCount.Should().Be(4);
        result.Value.SubmittedCount.Should().Be(2);
        result.Value.ShortlistedCount.Should().Be(0);
        result.Value.AcceptedCount.Should().Be(2);
        result.Value.RejectedCount.Should().Be(1);
        result.Value.WaitlistedCount.Should().Be(0);
        result.Value.WithdrawnCount.Should().Be(0);
    }

    [Fact]
    public void Given_CandidaciesForMultipleStudents_When_QueryingSummary_Then_ExcludesOtherStudents()
    {
        // Arrange
        var student = CreateStudent();
        var otherStudent = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Submitted));
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Rejected));
        candidacies.Seed(CreateCandidacy(otherStudent.Id, InternshipCandidacyStatus.Accepted));
        candidacies.Seed(CreateCandidacy(otherStudent.Id, InternshipCandidacyStatus.Withdrawn));

        var students = new FakeStudentRepository();
        students.Add(student);
        students.Add(otherStudent);
        var handler = new GetStudentCandidacySummaryHandler(students, candidacies);

        // Act
        var result = handler.Handle(new GetStudentCandidacySummaryQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.ActiveCount.Should().Be(1);
        result.Value.SubmittedCount.Should().Be(1);
        result.Value.RejectedCount.Should().Be(1);
        result.Value.AcceptedCount.Should().Be(0);
        result.Value.WithdrawnCount.Should().Be(0);
    }

    [Fact]
    public void Given_ExistingCandidacies_When_QueryingSummary_Then_DoesNotChangeRepositoryState()
    {
        // Arrange
        var student = CreateStudent();
        var candidacy = CreateCandidacy(student.Id, InternshipCandidacyStatus.Shortlisted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var beforeList = candidacies.Candidacies.ToArray();
        var beforeStates = candidacies.Candidacies
            .Select(item => (item.Id, item.StudentId, item.OfferId, item.ApplicationDate, item.Status))
            .ToArray();

        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(students, candidacies);

        // Act
        var result = handler.Handle(new GetStudentCandidacySummaryQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        candidacies.Candidacies.Should().Equal(beforeList);
        candidacies.Candidacies
            .Select(item => (item.Id, item.StudentId, item.OfferId, item.ApplicationDate, item.Status))
            .Should().Equal(beforeStates);
        candidacies.AddCallCount.Should().Be(0);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    private static Student CreateStudent() =>
        new(Guid.NewGuid(), "Test Student", "student@example.test");

    private static InternshipCandidacy CreateCandidacy(
        Guid studentId,
        InternshipCandidacyStatus status) =>
        new(
            Guid.NewGuid(),
            studentId,
            Guid.NewGuid(),
            ApplicationDate,
            status);
}
