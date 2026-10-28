using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.GetOffers;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;

namespace InternHub.Tests.Unit.Offers;

public sealed class GetOffersHandlerTests
{
    private static readonly DateTime FirstDeadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondDeadline =
        new(2027, 2, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_EmptyRepository_When_GettingOffers_Then_ReturnsEmptySuccessList()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new GetOffersHandler(repository);

        // Act
        var result = handler.Handle(new GetOffersQuery());

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_RepositoryOffers_When_GettingOffers_Then_ReturnsAllOfferResponses()
    {
        // Arrange
        var firstOffer = new Offer("Backend Intern", "Example Co", "C#", FirstDeadline, 2);
        var secondOffer = new Offer("Data Intern", "Other Co", "SQL", SecondDeadline, 1);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(firstOffer);
        repository.Offers.Add(secondOffer);
        var handler = new GetOffersHandler(repository);

        // Act
        var result = handler.Handle(new GetOffersQuery());

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(
            new[]
            {
                new GetOffersResponse(firstOffer.Id, "Backend Intern", "Example Co", "C#", FirstDeadline, 2, false, 0),
                new GetOffersResponse(secondOffer.Id, "Data Intern", "Other Co", "SQL", SecondDeadline, 1, false, 0)
            },
            options => options.WithStrictOrdering());
    }
}
