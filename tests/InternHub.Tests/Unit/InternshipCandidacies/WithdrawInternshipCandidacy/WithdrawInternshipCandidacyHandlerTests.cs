using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;
using InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies.WithdrawInternshipCandidacy;

public sealed class WithdrawInternshipCandidacyHandlerTests
{
    private const string StudentIdClaimType = "student_id";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_AnonymousRequest_When_Withdrawing_Then_ReturnsUnauthorizedWithoutCallingAtomicWithdrawal()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var unauthenticatedPrincipal = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var nullResult = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, null));
        var unauthenticatedResult = await handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId, unauthenticatedPrincipal));

        // Assert
        nullResult.Status.Should().Be(ResultStatus.Unauthorized);
        nullResult.Value.Should().BeFalse();
        unauthenticatedResult.Status.Should().Be(ResultStatus.Unauthorized);
        unauthenticatedResult.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiter_When_Withdrawing_Then_ReturnsForbiddenWithoutCallingAtomicWithdrawal()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
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
        var result = await handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId, recruiterPrincipal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_StudentWithoutStudentIdClaim_When_Withdrawing_Then_ReturnsForbiddenWithoutCallingAtomicWithdrawal()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId: null, sub: Guid.NewGuid().ToString());

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_StudentWithInvalidStudentIdClaim_When_Withdrawing_Then_ReturnsForbiddenWithoutCallingAtomicWithdrawal(
        string invalidStudentId)
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(invalidStudentId, sub: "account-student");

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_StudentWithDuplicateOrSplitIdentityClaims_When_Withdrawing_Then_ReturnsForbiddenWithoutCallingAtomicWithdrawal()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var duplicatePrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, studentId.ToString()),
                    new Claim(StudentIdClaimType, studentId.ToString())
                ],
                authenticationType: "TestScheme"));
        var splitPrincipal = new ClaimsPrincipal(
            [
                new ClaimsIdentity([new Claim(ClaimTypes.Role, "Student")], authenticationType: "SchemeA"),
                new ClaimsIdentity([new Claim(StudentIdClaimType, studentId.ToString())], authenticationType: "SchemeB")
            ]);

        // Act
        var duplicateResult = await handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId, duplicatePrincipal));
        var splitResult = await handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId, splitPrincipal));

        // Assert
        duplicateResult.Status.Should().Be(ResultStatus.Forbidden);
        splitResult.Status.Should().Be(ResultStatus.Forbidden);
        withdrawals.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_ValidStudentAndAtomicWithdrawalSucceeds_When_Withdrawing_Then_PassesAuthenticatedStudentIdAndReturnsSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacy = CreateCandidacy(studentId, InternshipCandidacyStatus.Withdrawn);
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Success(candidacy)
        };
        var guard = new FakeStudentAccessGuard
        {
            ResolveHandler = (_, _) => StudentAccessResult.Authorized(studentId)
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals, guard);
        var principal = CreateStudentPrincipal(studentId.ToString(), sub: "account-student-1");

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacy.Id, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        result.Error.Should().BeNull();
        guard.ResolveCallCount.Should().Be(1);
        guard.LastUser.Should().BeSameAs(principal);
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastCandidacyId.Should().Be(candidacy.Id);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_CancellationToken_When_Withdrawing_Then_PassesTokenToGuardAndAtomicWithdrawal()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var guard = new FakeStudentAccessGuard
        {
            ResolveHandler = (_, _) => StudentAccessResult.Authorized(studentId)
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals, guard);
        var principal = CreateStudentPrincipal(studentId.ToString());

        // Act
        await handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId, principal),
            cancellationSource.Token);

        // Assert
        guard.ResolveCallCount.Should().Be(1);
        guard.LastCancellationToken.Should().Be(cancellationSource.Token);
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastCandidacyId.Should().Be(candidacyId);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
        withdrawals.LastCancellationToken.Should().Be(cancellationSource.Token);
    }

    [Fact]
    public async Task Given_AtomicWithdrawalReturnsForbidden_When_Withdrawing_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Failure(
                AtomicCandidacyWithdrawalStatus.Forbidden)
        };
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString(), sub: "account-other");

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Error.Should().Be($"Authenticated student is not allowed to withdraw candidacy {candidacyId}.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Withdrawing_Then_ReturnsNotFound()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Failure(
                AtomicCandidacyWithdrawalStatus.CandidacyNotFound)
        };
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString());

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacyId} not found.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public async Task Given_InvalidTransition_When_Withdrawing_Then_ReturnsConflictWithCurrentStatus(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.InvalidTransition(finalStatus)
        };
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString());

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacyId} cannot be withdrawn from status {finalStatus}.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_ConcurrencyConflict_When_Withdrawing_Then_ReturnsConflictWithRetryMessage()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Failure(
                AtomicCandidacyWithdrawalStatus.ConcurrencyConflict)
        };
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString());

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacyId} could not be withdrawn due to a concurrent update. Retry the request.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_AtomicWithdrawalThrowsPersistenceError_When_Withdrawing_Then_PropagatesExceptionWithoutReturningSuccess()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            ExceptionToThrow = new InvalidOperationException("Simulated persistence failure.")
        };
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        var principal = CreateStudentPrincipal(studentId.ToString());

        // Act
        Func<Task> act = () => handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId, principal));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastAuthenticatedStudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_CanceledToken_When_Withdrawing_Then_ThrowsOperationCanceledExceptionWithoutCallingAtomicWithdrawal()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(
            withdrawals,
            new ClaimsPrincipalStudentAccessGuard());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = () => handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId, CreateStudentPrincipal(studentId.ToString())),
            cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        withdrawals.CallCount.Should().Be(0);
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
