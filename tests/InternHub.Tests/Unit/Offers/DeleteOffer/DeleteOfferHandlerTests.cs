using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.DeleteOffer;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;

namespace InternHub.Tests.Unit.Offers.DeleteOffer;

public sealed class DeleteOfferHandlerTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ExistingOffer_When_DeletingOffer_Then_ReturnsSuccessAndRemovesOffer()
    {
        // Arrange
        var offer = new Offer("Backend Intern", "Example Co", "C#", Deadline, 2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new DeleteOfferHandler(repository);

        // Act
        var result = handler.Handle(new DeleteOfferCommand(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
        repository.DeleteCallCount.Should().Be(1);
        repository.Offers.Should().BeEmpty();
    }

    [Fact]
    public void Given_MissingOffer_When_DeletingOffer_Then_ReturnsNotFound()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new DeleteOfferHandler(repository);
        var offerId = Guid.NewGuid();

        // Act
        var result = handler.Handle(new DeleteOfferCommand(offerId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offerId.ToString());
        repository.DeleteCallCount.Should().Be(1);
        repository.Offers.Should().BeEmpty();
    }
}
