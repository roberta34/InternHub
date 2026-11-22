using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;

namespace InternHub.Tests.Unit.InternshipCandidacies;

public sealed class DecideInternshipCandidacyHandlerTests
{
    private static readonly DateTime ApplicationDate = new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("1")]
    [InlineData("Shortlist,Accept")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("foo")]
    public async Task Given_InvalidAction_When_Deciding_Then_ReturnsValidationAndDoesNotCallAtomicDecision(string? action)
    {
        // Arrange
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(Guid.NewGuid(), action);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Be(
            $"Action '{action}' is not valid. Allowed values: Shortlist, Accept, Reject.");
        decisions.CallCount.Should().Be(0);
    }

    [Theory]
    [InlineData("Shortlist", CandidacyDecision.Shortlist)]
    [InlineData("shortlist", CandidacyDecision.Shortlist)]
    [InlineData("ACCEPT", CandidacyDecision.Accept)]
    [InlineData("Accept", CandidacyDecision.Accept)]
    [InlineData("reject", CandidacyDecision.Reject)]
    public async Task Given_ValidActionIgnoringCase_When_Deciding_Then_PassesParsedDecisionAndIdToAtomicDecision(
        string action,
        CandidacyDecision expectedDecision)
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(candidacyId, action);

        // Act
        await handler.HandleAsync(command);

        // Assert
        decisions.CallCount.Should().Be(1);
        decisions.LastCandidacyId.Should().Be(candidacyId);
        decisions.LastDecision.Should().Be(expectedDecision);
    }

    [Fact]
    public async Task Given_CancellationToken_When_Deciding_Then_PassesTokenToAtomicDecision()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(Guid.NewGuid(), "Reject");

        // Act
        await handler.HandleAsync(command, cancellationSource.Token);

        // Assert
        decisions.LastCancellationToken.Should().Be(cancellationSource.Token);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    [InlineData(InternshipCandidacyStatus.Accepted)]
    [InlineData(InternshipCandidacyStatus.Waitlisted)]
    [InlineData(InternshipCandidacyStatus.Rejected)]
    public async Task Given_AtomicDecisionDecided_When_Deciding_Then_ReturnsSuccessWithCandidacyIdAndStatus(
        InternshipCandidacyStatus decidedStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(decidedStatus);
        var decisions = new FakeAtomicCandidacyDecision
        {
            Result = AtomicCandidacyDecisionResult.Success(candidacy)
        };
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(candidacy.Id, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().Be(new DecideInternshipCandidacyResponse(candidacy.Id, decidedStatus));
    }

    [Fact]
    public async Task Given_AtomicDecisionCandidacyNotFound_When_Deciding_Then_ReturnsNotFound()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision
        {
            Result = AtomicCandidacyDecisionResult.Failure(AtomicCandidacyDecisionStatus.CandidacyNotFound)
        };
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Reject");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacyId} not found.");
    }

    [Fact]
    public async Task Given_AtomicDecisionOfferNotFound_When_Deciding_Then_ReturnsNotFound()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision
        {
            Result = AtomicCandidacyDecisionResult.Failure(AtomicCandidacyDecisionStatus.OfferNotFound)
        };
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Offer of candidacy {candidacyId} not found.");
    }

    [Theory]
    [InlineData("Shortlist", "shortlisted")]
    [InlineData("Accept", "accepted")]
    [InlineData("Reject", "rejected")]
    public async Task Given_AtomicDecisionInvalidTransition_When_Deciding_Then_ReturnsConflictWithActionVerb(
        string action,
        string verb)
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision
        {
            Result = AtomicCandidacyDecisionResult.Failure(AtomicCandidacyDecisionStatus.InvalidTransition)
        };
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(candidacyId, action);

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be($"Candidacy {candidacyId} cannot be {verb} from its current status.");
    }

    [Fact]
    public async Task Given_AtomicDecisionConcurrencyConflict_When_Deciding_Then_ReturnsConflictWithRetryMessage()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision
        {
            Result = AtomicCandidacyDecisionResult.Failure(AtomicCandidacyDecisionStatus.ConcurrencyConflict)
        };
        var handler = new DecideInternshipCandidacyHandler(decisions);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacyId} could not be decided because the offer was modified concurrently. Retry the request.");
    }

    private static InternshipCandidacy CreateCandidacy(InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ApplicationDate, status);
}
