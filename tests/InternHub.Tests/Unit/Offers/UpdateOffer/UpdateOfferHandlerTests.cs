using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.UpdateOffer;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Tests.Unit.InternshipCandidacies.Fakes;

namespace InternHub.Tests.Unit.Offers.UpdateOffer;

public sealed class UpdateOfferHandlerTests
{
    private static readonly Guid RecruiterId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ExistingOfferWithCurrentVersion_When_UpdatingOffer_Then_ReturnsSuccessAndPersistsChanges()
    {
        // Arrange
        var offer = new Offer("Old title", "Old company", "Old requirements", Deadline, 1, RecruiterId);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "New title",
            "New company",
            "New requirements",
            new DateTime(2027, 2, 20, 0, 0, 0, DateTimeKind.Utc),
            4,
            offer.Version,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        repository.UpdateCallCount.Should().Be(1);
        offer.Title.Should().Be("New title");
        offer.CompanyName.Should().Be("New company");
        offer.Requirements.Should().Be("New requirements");
        offer.MaximumAvailableSlots.Should().Be(4);
        offer.Version.Should().Be(1);
    }

    [Fact]
    public void Given_InvalidOfferData_When_UpdatingOffer_Then_ReturnsValidationWithoutPersisting()
    {
        // Arrange
        var offer = new Offer("Old title", "Old company", "Old requirements", Deadline, 1, RecruiterId);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "",
            "New company",
            "New requirements",
            Deadline,
            4,
            offer.Version,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("Title is required.");
        repository.UpdateCallCount.Should().Be(0);
        offer.Title.Should().Be("Old title");
        offer.Version.Should().Be(0);
    }

    [Fact]
    public void Given_MissingOffer_When_UpdatingOffer_Then_ReturnsNotFoundWithoutPersisting()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new UpdateOfferHandler(repository);
        var offerId = Guid.NewGuid();
        var command = new UpdateOfferCommand(
            offerId,
            "New title",
            "New company",
            "New requirements",
            Deadline,
            4,
            0,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offerId.ToString());
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_StaleOfferVersion_When_UpdatingOffer_Then_ReturnsConflictWithoutPersisting()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 2, RecruiterId);
        offer.Update("Newer title", "Current company", "Current requirements", Deadline, 2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "Stale title",
            "Stale company",
            "Stale requirements",
            Deadline,
            3,
            0,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("stale version");
        repository.UpdateCallCount.Should().Be(0);
        offer.Title.Should().Be("Newer title");
        offer.Version.Should().Be(1);
    }

    [Fact]
    public void Given_ConcurrentRepositoryUpdate_When_UpdatingOffer_Then_ReturnsConflict()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 2, RecruiterId);
        var repository = new FakeOfferRepository
        {
            UpdateException = new OfferConcurrencyException(offer.Id, new Exception("concurrent update"))
        };
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "New title",
            "New company",
            "New requirements",
            Deadline,
            3,
            offer.Version,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("modified concurrently");
        repository.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_RepositoryCannotUpdateOffer_When_UpdatingOffer_Then_ReturnsNotFound()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 2, RecruiterId);
        var repository = new FakeOfferRepository { UpdateResult = false };
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "New title",
            "New company",
            "New requirements",
            Deadline,
            3,
            offer.Version,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offer.Id.ToString());
        repository.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_CapacityBelowAcceptedCount_When_UpdatingOffer_Then_ReturnsConflictAndPreservesState()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 3, RecruiterId);
        var offerRepository = new FakeOfferRepository();
        offerRepository.Offers.Add(offer);
        var candidacyRepository = new FakeInternshipCandidacyRepository();
        candidacyRepository.Seed(new InternshipCandidacy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            offer.Id,
            Deadline,
            InternshipCandidacyStatus.Accepted));
        candidacyRepository.Seed(new InternshipCandidacy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            offer.Id,
            Deadline,
            InternshipCandidacyStatus.Accepted));
        var handler = new UpdateOfferHandler(offerRepository, candidacyRepository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "New title",
            "New company",
            "New requirements",
            Deadline,
            1,
            offer.Version,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        offerRepository.UpdateCallCount.Should().Be(0);
        offer.Title.Should().Be("Current title");
        offer.MaximumAvailableSlots.Should().Be(3);
        offer.Version.Should().Be(0);
    }

    [Fact]
    public void Given_CapacityEqualToAcceptedCount_When_UpdatingOffer_Then_ReturnsSuccess()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 1, RecruiterId);
        var offerRepository = new FakeOfferRepository();
        offerRepository.Offers.Add(offer);
        var candidacyRepository = new FakeInternshipCandidacyRepository();
        candidacyRepository.Seed(new InternshipCandidacy(
            Guid.NewGuid(),
            Guid.NewGuid(),
            offer.Id,
            Deadline,
            InternshipCandidacyStatus.Accepted));
        var handler = new UpdateOfferHandler(offerRepository, candidacyRepository);
        var command = new UpdateOfferCommand(
            offer.Id,
            "New title",
            "New company",
            "New requirements",
            Deadline,
            1,
            offer.Version,
            RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        offer.MaximumAvailableSlots.Should().Be(1);
        offer.Version.Should().Be(1);
    }

    [Fact]
    public void Given_UnauthenticatedCommand_When_UpdatingOffer_Then_ReturnsUnauthorized()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id, "New title", "New company", "New requirements", Deadline, 3, offer.Version);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_DifferentRecruiter_When_UpdatingOffer_Then_ReturnsForbiddenAndPreservesState()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 2, RecruiterId);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var otherRecruiterId = Guid.NewGuid();
        var command = new UpdateOfferCommand(
            offer.Id, "New title", "New company", "New requirements", Deadline, 3, offer.Version, otherRecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        repository.UpdateCallCount.Should().Be(0);
        offer.Title.Should().Be("Current title");
        offer.Version.Should().Be(0);
    }

    [Fact]
    public void Given_LegacyOfferWithoutOwner_When_UpdatingOffer_Then_ReturnsForbidden()
    {
        // Arrange
        var offer = new Offer("Current title", "Current company", "Current requirements", Deadline, 2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new UpdateOfferHandler(repository);
        var command = new UpdateOfferCommand(
            offer.Id, "New title", "New company", "New requirements", Deadline, 3, offer.Version, RecruiterId);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Forbidden);
        repository.UpdateCallCount.Should().Be(0);
        offer.Title.Should().Be("Current title");
        offer.Version.Should().Be(0);
    }
}
