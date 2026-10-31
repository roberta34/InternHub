using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.CloseOffer;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;

namespace InternHub.Tests.Unit.Offers;

public sealed class CloseOfferHandlerTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ExistingOpenOffer_When_ClosingOffer_Then_ReturnsSuccessAndUpdatesOnce()
    {
        // Arrange
        var offer = CreateOffer();
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new CloseOfferHandler(repository);

        // Act
        var result = handler.Handle(new CloseOfferCommand(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        offer.IsClosed.Should().BeTrue();
        repository.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_AlreadyClosedOffer_When_ClosingOffer_Then_ReturnsSuccessWithoutUpdating()
    {
        // Arrange
        var offer = CreateOffer();
        offer.Close();
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new CloseOfferHandler(repository);

        // Act
        var result = handler.Handle(new CloseOfferCommand(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_MissingOffer_When_ClosingOffer_Then_ReturnsNotFound()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new CloseOfferHandler(repository);
        var offerId = Guid.NewGuid();

        // Act
        var result = handler.Handle(new CloseOfferCommand(offerId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offerId.ToString());
        repository.UpdateCallCount.Should().Be(0);
    }

    [Fact]
    public void Given_ExistingOpenOfferAndFailedUpdate_When_ClosingOffer_Then_ReturnsNotFound()
    {
        // Arrange
        var offer = CreateOffer();
        var repository = new FakeOfferRepository
        {
            UpdateResult = false
        };
        repository.Offers.Add(offer);
        var handler = new CloseOfferHandler(repository);

        // Act
        var result = handler.Handle(new CloseOfferCommand(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offer.Id.ToString());
        repository.UpdateCallCount.Should().Be(1);
    }

    [Fact]
    public void Given_ExistingOpenOfferAndConcurrencyException_When_ClosingOffer_Then_ReturnsConflict()
    {
        // Arrange
        var offer = CreateOffer();
        var repository = new FakeOfferRepository
        {
            UpdateException = new OfferConcurrencyException(
                offer.Id,
                new InvalidOperationException("Concurrency conflict"))
        };
        repository.Offers.Add(offer);
        var handler = new CloseOfferHandler(repository);

        // Act
        var result = handler.Handle(new CloseOfferCommand(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("modified concurrently");
        repository.UpdateCallCount.Should().Be(1);
    }

    private static Offer CreateOffer() =>
        new("Backend Intern", "Example Company", "C#, Git", Deadline, 2);
}
