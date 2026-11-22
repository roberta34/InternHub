using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.AtomicDecision;
using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;
using InternHub.Tests.Unit.Offers.Fakes;

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
        var handler = CreateHandler(decisions, Guid.NewGuid());
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
        var handler = CreateHandler(decisions, candidacyId);
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
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = CreateHandler(decisions, candidacyId);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Reject");

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
        var handler = CreateHandler(decisions, candidacy.Id);
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
        var handler = CreateHandler(decisions, candidacyId);
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
        var handler = CreateHandler(decisions, candidacyId);
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
        var handler = CreateHandler(decisions, candidacyId);
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
        var handler = CreateHandler(decisions, candidacyId);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be(
            $"Candidacy {candidacyId} could not be decided because the offer was modified concurrently. Retry the request.");
    }

    [Fact]
    public async Task Given_AnonymousUserAndInvalidAction_When_Deciding_Then_ReturnsUnauthorizedAndDoesNotCallAtomicDecision()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var guard = FakeRecruiterAccessGuard.Unauthorized();
        var handler = CreateHandler(decisions, candidacyId, guard);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "foo");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        decisions.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_NonRecruiterAndInvalidAction_When_Deciding_Then_ReturnsForbiddenAndDoesNotCallAtomicDecision()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var guard = FakeRecruiterAccessGuard.Forbidden();
        var handler = CreateHandler(decisions, candidacyId, guard);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "foo");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        decisions.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_UserFromCommand_When_Deciding_Then_PassesItToRecruiterAccessGuard()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var guard = FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId);
        var handler = CreateHandler(new FakeAtomicCandidacyDecision(), candidacyId, guard);
        var user = new System.Security.Claims.ClaimsPrincipal();
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Reject", user);

        // Act
        await handler.HandleAsync(command);

        // Assert
        guard.CallCount.Should().Be(1);
        guard.LastUser.Should().BeSameAs(user);
    }

    [Fact]
    public async Task Given_MissingCandidacy_When_Deciding_Then_ReturnsNotFoundAndDoesNotCallAtomicDecision()
    {
        // Arrange
        var missingCandidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = new DecideInternshipCandidacyHandler(
            decisions,
            FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId),
            new FakeInternshipCandidacyRepository(),
            new FakeOfferRepository());
        var command = new DecideInternshipCandidacyCommand(missingCandidacyId, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {missingCandidacyId} not found.");
        decisions.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_CandidacyWhoseOfferIsMissing_When_Deciding_Then_ReturnsNotFoundAndDoesNotCallAtomicDecision()
    {
        // Arrange
        var candidacy = CreateCandidacy(InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = new DecideInternshipCandidacyHandler(
            decisions,
            FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId),
            candidacies,
            new FakeOfferRepository());
        var command = new DecideInternshipCandidacyCommand(candidacy.Id, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Offer of candidacy {candidacy.Id} not found.");
        decisions.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_OfferOwnedByAnotherRecruiter_When_Deciding_Then_ReturnsForbiddenAndDoesNotCallAtomicDecision()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = CreateHandler(decisions, candidacyId, offerRecruiterId: Guid.NewGuid());
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        result.Error.Should().Be($"Only the owning Recruiter can decide candidacy {candidacyId}.");
        decisions.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_LegacyOfferWithoutOwner_When_Deciding_Then_ReturnsForbiddenAndDoesNotCallAtomicDecision()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var decisions = new FakeAtomicCandidacyDecision();
        var handler = CreateHandler(decisions, candidacyId, offerRecruiterId: null);
        var command = new DecideInternshipCandidacyCommand(candidacyId, "Accept");

        // Act
        var result = await handler.HandleAsync(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        decisions.CallCount.Should().Be(0);
    }

    private static readonly Guid RecruiterId = Guid.NewGuid();

    private static DecideInternshipCandidacyHandler CreateHandler(
        FakeAtomicCandidacyDecision decisions,
        Guid candidacyId,
        FakeRecruiterAccessGuard? guard = null) =>
        CreateHandler(decisions, candidacyId, RecruiterId, guard);

    private static DecideInternshipCandidacyHandler CreateHandler(
        FakeAtomicCandidacyDecision decisions,
        Guid candidacyId,
        Guid? offerRecruiterId,
        FakeRecruiterAccessGuard? guard = null)
    {
        var offer = offerRecruiterId is null
            ? new Offer("Offer", "Company", ".NET", ApplicationDate.AddDays(10), 1)
            : new Offer("Offer", "Company", ".NET", ApplicationDate.AddDays(10), 1, offerRecruiterId);
        var offers = new FakeOfferRepository();
        offers.Offers.Add(offer);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(new InternshipCandidacy(
            candidacyId, Guid.NewGuid(), offer.Id, ApplicationDate, InternshipCandidacyStatus.Submitted));

        return new DecideInternshipCandidacyHandler(
            decisions,
            guard ?? FakeRecruiterAccessGuard.AuthorizedAs(RecruiterId),
            candidacies,
            offers);
    }

    private static InternshipCandidacy CreateCandidacy(InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), ApplicationDate, status);
}
