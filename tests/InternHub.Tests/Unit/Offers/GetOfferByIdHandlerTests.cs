using FluentAssertions;
using InternHub.Api.Features.Offers.GetOfferById;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;
using InternHub.Api.Domain.Offers;

namespace InternHub.Tests.Unit.Offers;

public sealed class GetOfferByIdHandlerTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ExistingOfferId_When_GettingOfferById_Then_ReturnsOfferResponse()
    {
        // Arrange
        var offer = new Offer("Backend Intern", "Example Co", "C#", Deadline, 2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new GetOfferByIdHandler(repository);

        // Act
        var result = handler.Handle(new GetOfferByIdQuery(offer.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(new GetOfferByIdResponse(
            offer.Id, "Backend Intern", "Example Co", "C#", Deadline, 2, false, 0));
    }

    [Fact]
    public void Given_UnknownOfferId_When_GettingOfferById_Then_ReturnsNotFound()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new GetOfferByIdHandler(repository);
        var offerId = Guid.NewGuid();

        // Act
        var result = handler.Handle(new GetOfferByIdQuery(offerId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(offerId.ToString());
        result.Value.Should().BeNull();
    }
}
