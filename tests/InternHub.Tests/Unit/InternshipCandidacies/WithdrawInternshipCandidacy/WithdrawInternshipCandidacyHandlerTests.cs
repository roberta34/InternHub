using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies.WithdrawInternshipCandidacy;

public sealed class WithdrawInternshipCandidacyHandlerTests
{
    private static readonly DateTime ApplicationDate =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public void Given_OpenCandidacy_When_Withdrawing_Then_ReturnsSuccessAndUpdates(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(initialStatus);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var handler = new WithdrawInternshipCandidacyHandler(candidacies);

        // Act
        var result = handler.Handle(new WithdrawInternshipCandidacyCommand(candidacy.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Withdrawn)]
    public void Given_FinalCandidacy_When_Withdrawing_Then_ReturnsConflictAndDoesNotUpdate(
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(finalStatus);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var handler = new WithdrawInternshipCandidacyHandler(candidacies);

        // Act
        var result = handler.Handle(new WithdrawInternshipCandidacyCommand(candidacy.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacy.Id} cannot be withdrawn from status {finalStatus}.");
        candidacy.Status.Should().Be(finalStatus);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_MissingCandidacy_When_Withdrawing_Then_ReturnsNotFoundAndDoesNotUpdate()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new WithdrawInternshipCandidacyHandler(candidacies);

        // Act
        var result = handler.Handle(new WithdrawInternshipCandidacyCommand(candidacyId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacyId} not found.");
        result.Value.Should().BeFalse();
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_OpenCandidacyAndFailedRepositoryUpdate_When_Withdrawing_Then_ReturnsNotFound()
    {
        // Arrange
        var candidacy = CreateCandidacy(InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository { UpdateResult = false };
        candidacies.Seed(candidacy);
        var handler = new WithdrawInternshipCandidacyHandler(candidacies);

        // Act
        var result = handler.Handle(new WithdrawInternshipCandidacyCommand(candidacy.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacy.Id} not found.");
        result.Value.Should().BeFalse();
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Withdrawn);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    private static InternshipCandidacy CreateCandidacy(InternshipCandidacyStatus status) =>
        new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            ApplicationDate,
            status);
}
