using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
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
    public void Given_InvalidAction_When_Deciding_Then_ReturnsValidationAndDoesNotUpdate(string? action)
    {
        // Arrange
        var candidacy = CreateCandidacy(Guid.NewGuid(), InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var handler = new DecideInternshipCandidacyHandler(candidacies, new FakeOfferRepository());

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, action));

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("Allowed values: Shortlist, Accept, Reject.");
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_MissingCandidacy_When_Deciding_Then_ReturnsNotFound()
    {
        // Arrange
        var candidacyId = Guid.NewGuid();
        var candidacies = new FakeInternshipCandidacyRepository();
        var handler = new DecideInternshipCandidacyHandler(candidacies, new FakeOfferRepository());

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacyId, "Reject"));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacyId} not found.");
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_SubmittedCandidacy_When_ShortlistDecided_Then_ReturnsShortlistedAndUpdates()
    {
        // Arrange
        var candidacy = CreateCandidacy(Guid.NewGuid(), InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var handler = new DecideInternshipCandidacyHandler(candidacies, new FakeOfferRepository());

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "Shortlist"));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().Be(new DecideInternshipCandidacyResponse(
            candidacy.Id,
            InternshipCandidacyStatus.Shortlisted));
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Shortlisted);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_UppercaseAcceptAndFreeSlot_When_Deciding_Then_ReturnsAccepted()
    {
        // Arrange
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var candidacy = CreateCandidacy(offer.Id, InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new DecideInternshipCandidacyHandler(candidacies, offers);

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "ACCEPT"));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_AcceptancesReachedMaximumSlots_When_AcceptDecided_Then_ReturnsWaitlisted()
    {
        // Arrange
        var offer = CreateOffer(maximumAvailableSlots: 1);
        var alreadyAccepted = CreateCandidacy(offer.Id, InternshipCandidacyStatus.Accepted);
        var candidacy = CreateCandidacy(offer.Id, InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(alreadyAccepted);
        candidacies.Seed(candidacy);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new DecideInternshipCandidacyHandler(candidacies, offers);

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "Accept"));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.Status.Should().Be(InternshipCandidacyStatus.Waitlisted);
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Waitlisted);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(InternshipCandidacyStatus.Submitted)]
    [InlineData(InternshipCandidacyStatus.Shortlisted)]
    public void Given_OpenCandidacy_When_RejectDecided_Then_ReturnsRejected(
        InternshipCandidacyStatus initialStatus)
    {
        // Arrange
        var candidacy = CreateCandidacy(Guid.NewGuid(), initialStatus);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var handler = new DecideInternshipCandidacyHandler(candidacies, new FakeOfferRepository());

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "Reject"));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.Status.Should().Be(InternshipCandidacyStatus.Rejected);
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Rejected);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    [Theory]
    [InlineData("Shortlist", "shortlisted", InternshipCandidacyStatus.Accepted)]
    [InlineData("Shortlist", "shortlisted", InternshipCandidacyStatus.Rejected)]
    [InlineData("Shortlist", "shortlisted", InternshipCandidacyStatus.Waitlisted)]
    [InlineData("Shortlist", "shortlisted", InternshipCandidacyStatus.Withdrawn)]
    [InlineData("Accept", "accepted", InternshipCandidacyStatus.Accepted)]
    [InlineData("Accept", "accepted", InternshipCandidacyStatus.Rejected)]
    [InlineData("Accept", "accepted", InternshipCandidacyStatus.Waitlisted)]
    [InlineData("Accept", "accepted", InternshipCandidacyStatus.Withdrawn)]
    [InlineData("Reject", "rejected", InternshipCandidacyStatus.Accepted)]
    [InlineData("Reject", "rejected", InternshipCandidacyStatus.Rejected)]
    [InlineData("Reject", "rejected", InternshipCandidacyStatus.Waitlisted)]
    [InlineData("Reject", "rejected", InternshipCandidacyStatus.Withdrawn)]
    public void Given_FinalCandidacy_When_Deciding_Then_ReturnsConflictAndDoesNotUpdate(
        string action,
        string verb,
        InternshipCandidacyStatus finalStatus)
    {
        // Arrange
        var offer = CreateOffer(maximumAvailableSlots: 5);
        var candidacy = CreateCandidacy(offer.Id, finalStatus);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new DecideInternshipCandidacyHandler(candidacies, offers);

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, action));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Be($"Candidacy {candidacy.Id} cannot be {verb} from status {finalStatus}.");
        candidacy.Status.Should().Be(finalStatus);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_AcceptAndMissingOffer_When_Deciding_Then_ReturnsNotFoundAndDoesNotUpdate()
    {
        // Arrange
        var candidacy = CreateCandidacy(Guid.NewGuid(), InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var handler = new DecideInternshipCandidacyHandler(candidacies, new FakeOfferRepository());

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "Accept"));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Offer {candidacy.OfferId} not found.");
        candidacy.Status.Should().Be(InternshipCandidacyStatus.Submitted);
        candidacies.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_RepositoryUpdateFails_When_Deciding_Then_ReturnsNotFound()
    {
        // Arrange
        var candidacy = CreateCandidacy(Guid.NewGuid(), InternshipCandidacyStatus.Submitted);
        var candidacies = new FakeInternshipCandidacyRepository { UpdateResult = false };
        candidacies.Seed(candidacy);
        var handler = new DecideInternshipCandidacyHandler(candidacies, new FakeOfferRepository());

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "Shortlist"));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Candidacy {candidacy.Id} not found.");
        candidacies.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_ClosedOffer_When_AcceptDecided_Then_DecisionIsNotBlocked()
    {
        // Arrange
        var offer = CreateOffer(maximumAvailableSlots: 1);
        offer.Close();
        var candidacy = CreateCandidacy(offer.Id, InternshipCandidacyStatus.Shortlisted);
        var candidacies = new FakeInternshipCandidacyRepository();
        candidacies.Seed(candidacy);
        var offers = new FakeOfferRepository();
        offers.Add(offer);
        var handler = new DecideInternshipCandidacyHandler(candidacies, offers);

        // Act
        var result = handler.Handle(new DecideInternshipCandidacyCommand(candidacy.Id, "Accept"));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.Status.Should().Be(InternshipCandidacyStatus.Accepted);
        candidacies.UpdateCallCount.Should().Be(1);
    }

    private static Offer CreateOffer(int maximumAvailableSlots) =>
        new("Offer", "Company", ".NET", ApplicationDate.AddDays(10), maximumAvailableSlots);

    private static InternshipCandidacy CreateCandidacy(Guid offerId, InternshipCandidacyStatus status) =>
        new(Guid.NewGuid(), Guid.NewGuid(), offerId, ApplicationDate, status);
}
