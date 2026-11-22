using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.AtomicWithdrawal;
using InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies.WithdrawInternshipCandidacy;

public sealed class WithdrawInternshipCandidacyHandlerTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Given_AtomicWithdrawalSucceeds_When_Withdrawing_Then_ReturnsSuccessAndDelegatesToAtomicWithdrawal()
    {
        // Arrange
        var candidacy = CreateCandidacy(InternshipCandidacyStatus.Withdrawn);
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Success(candidacy)
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals);

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacy.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        result.Error.Should().BeNull();
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastCandidacyId.Should().Be(candidacy.Id);
    }

    [Fact]
    public async Task Given_CancellationToken_When_Withdrawing_Then_PassesTokenToAtomicWithdrawal()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal();
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals);

        // Act
        await handler.HandleAsync(
            new WithdrawInternshipCandidacyCommand(candidacyId),
            cancellationSource.Token);

        // Assert
        withdrawals.CallCount.Should().Be(1);
        withdrawals.LastCandidacyId.Should().Be(candidacyId);
        withdrawals.LastCancellationToken.Should().Be(cancellationSource.Token);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Withdrawing_Then_ReturnsNotFound()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Failure(
                AtomicCandidacyWithdrawalStatus.CandidacyNotFound)
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals);

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacyId} not found.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
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
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.InvalidTransition(finalStatus)
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals);

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacyId} cannot be withdrawn from status {finalStatus}.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_ConcurrencyConflict_When_Withdrawing_Then_ReturnsConflictWithRetryMessage()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            Result = AtomicCandidacyWithdrawalResult.Failure(
                AtomicCandidacyWithdrawalStatus.ConcurrencyConflict)
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals);

        // Act
        var result = await handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacyId} could not be withdrawn due to a concurrent update. Retry the request.");
        result.Value.Should().BeFalse();
        withdrawals.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_AtomicWithdrawalThrowsPersistenceError_When_Withdrawing_Then_PropagatesExceptionWithoutReturningSuccess()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var withdrawals = new FakeAtomicCandidacyWithdrawal
        {
            ExceptionToThrow = new InvalidOperationException("Simulated persistence failure.")
        };
        var handler = new WithdrawInternshipCandidacyHandler(withdrawals);

        // Act
        Func<Task> act = () => handler.HandleAsync(new WithdrawInternshipCandidacyCommand(candidacyId));

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
        withdrawals.CallCount.Should().Be(1);
    }

    private static InternshipCandidacy CreateCandidacy(InternshipCandidacyStatus status) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ApplicationDate,
            status);
}
