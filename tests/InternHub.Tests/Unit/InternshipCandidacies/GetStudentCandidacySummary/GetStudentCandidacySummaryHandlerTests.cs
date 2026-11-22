using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies.GetStudentCandidacySummary;

public sealed class GetStudentCandidacySummaryHandlerTests
{
    private const string StudentIdClaimType = "student_id";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_AuthenticatedStudentWithMissingRecord_When_QueryingOwnSummary_Then_ReturnsNotFoundWithoutDto()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var students = new FakeStudentRepository();
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString(), sub: "account-missing");

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(studentId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Given_ExistingStudentWithoutCandidacies_When_QueryingOwnSummary_Then_ReturnsZeroCounts()
    {
        // Arrange
        var student = CreateStudent();
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            new FakeInternshipCandidacyRepository(),
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, principal));

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
    public async Task Given_StudentWithAllStatuses_When_QueryingOwnSummary_Then_ReturnsCorrectCountsAndActivity()
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
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString(), sub: "account-student-all");

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, principal));

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
    public async Task Given_MultipleCandidaciesWithSameStatus_When_QueryingOwnSummary_Then_AggregatesEachStatus()
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
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, principal));

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
    public async Task Given_CandidaciesForMultipleStudents_When_QueryingOwnSummary_Then_ExcludesOtherStudents()
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
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, principal));

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
    public async Task Given_ExistingCandidacies_When_QueryingOwnSummary_Then_DoesNotChangeRepositoryState()
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
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        candidacies.Candidacies.Should().Equal(beforeList);
        candidacies.Candidacies
            .Select(item => (item.Id, item.StudentId, item.OfferId, item.ApplicationDate, item.Status))
            .Should().Equal(beforeStates);
        candidacies.AddCallCount.Should().Be(0);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_UnauthenticatedRequest_When_QueryingSummary_Then_ReturnsUnauthorizedWithoutTouchingRepositories()
    {
        // Arrange
        var student = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Submitted));
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());

        // Act
        var result = await handler.HandleAsync(
            new GetStudentCandidacySummaryQuery(student.Id, new ClaimsPrincipal(new ClaimsIdentity())));

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_StudentA_When_QueryingStudentBSummary_Then_ReturnsForbiddenWithoutTouchingRepositories()
    {
        // Arrange
        var studentA = CreateStudent();
        var studentB = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(studentB.Id, InternshipCandidacyStatus.Accepted));
        var students = new FakeStudentRepository();
        students.Add(studentA);
        students.Add(studentB);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principalA = CreateStudentPrincipal(studentA.Id.ToString(), sub: "account-a");

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(studentB.Id, principalA));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiter_When_QueryingStudentSummary_Then_ReturnsForbiddenWithoutTouchingRepositories()
    {
        // Arrange
        var student = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(CreateCandidacy(student.Id, InternshipCandidacyStatus.Submitted));
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var recruiterPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, Guid.NewGuid().ToString()),
                    new Claim(SubjectClaimType, "account-recruiter")
                ],
                authenticationType: "TestScheme"));

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, recruiterPrincipal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_InvalidStudentIdentityClaim_When_QueryingSummary_Then_ReturnsForbiddenWithoutTouchingRepositories(
        string? invalidStudentId)
    {
        // Arrange
        var student = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(invalidStudentId, sub: student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeNull();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_DuplicateOrCrossIdentityStudentClaims_When_QueryingSummary_Then_ReturnsForbiddenWithoutTouchingRepositories()
    {
        // Arrange
        var student = CreateStudent();
        var candidacies = new FakeInternshipCandidacyRepository();
        var students = new FakeStudentRepository();
        students.Add(student);
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        var splitPrincipal = new ClaimsPrincipal(
            [
                new ClaimsIdentity([new Claim(ClaimTypes.Role, "Student")], authenticationType: "SchemeA"),
                new ClaimsIdentity([new Claim(StudentIdClaimType, student.Id.ToString())], authenticationType: "SchemeB")
            ]);
        var duplicatePrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, student.Id.ToString()),
                    new Claim(StudentIdClaimType, student.Id.ToString())
                ],
                authenticationType: "TestScheme"));

        // Act
        var splitResult = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, splitPrincipal));
        var duplicateResult = await handler.HandleAsync(new GetStudentCandidacySummaryQuery(student.Id, duplicatePrincipal));

        // Assert
        splitResult.Status.Should().Be(ResultStatus.Forbidden);
        duplicateResult.Status.Should().Be(ResultStatus.Forbidden);
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthorizedSummaryQueryWithCancellationToken_When_Handling_Then_PropagatesCancellationTokenToAccessGuard()
    {
        // Arrange
        var student = CreateStudent();
        var students = new FakeStudentRepository();
        students.Add(student);
        var guard = new FakeStudentAccessGuard();
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            new FakeInternshipCandidacyRepository(),
            guard);
        using var cancellation = new CancellationTokenSource();
        var principal = CreateStudentPrincipal(student.Id.ToString());

        // Act
        var result = await handler.HandleAsync(
            new GetStudentCandidacySummaryQuery(student.Id, principal),
            cancellation.Token);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        guard.AuthorizeCallCount.Should().Be(1);
        guard.LastRequestedStudentId.Should().Be(student.Id);
        guard.LastCancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    public async Task Given_CanceledToken_When_QueryingSummary_Then_ThrowsOperationCanceledExceptionWithoutTouchingRepositories()
    {
        // Arrange
        var student = CreateStudent();
        var students = new FakeStudentRepository();
        students.Add(student);
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new GetStudentCandidacySummaryHandler(
            students,
            candidacies,
            new ClaimsPrincipalStudentAccessGuard());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = async () => await handler.HandleAsync(
            new GetStudentCandidacySummaryQuery(student.Id, CreateStudentPrincipal(student.Id.ToString())),
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        students.GetByIdCallCount.Should().Be(0);
        candidacies.GetByStudentCallCount.Should().Be(0);
    }

    private static ClaimsPrincipal CreateStudentPrincipal(
        string? studentId,
        string? sub = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Student") };

        if (studentId is not null)
        {
            claims.Add(new Claim(StudentIdClaimType, studentId));
        }

        if (sub is not null)
        {
            claims.Add(new Claim(SubjectClaimType, sub));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestScheme"));
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
