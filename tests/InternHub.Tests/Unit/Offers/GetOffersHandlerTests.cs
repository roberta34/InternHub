using FluentAssertions;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Offers.GetOffers;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;

namespace InternHub.Tests.Unit.Offers;

public sealed class GetOffersHandlerTests
{
    private static readonly DateTime CurrentUtc =
        new(2027, 1, 15, 12, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime FirstDeadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime SecondDeadline =
        new(2027, 2, 20, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_EmptyRepository_When_GettingOffers_Then_ReturnsEmptySuccessList()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new GetOffersHandler(
            repository,
            TimeProvider.System);

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
        var handler = new GetOffersHandler(
            repository,
            TimeProvider.System);

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

    [Fact]
    public void Given_OpenOfferWithFutureDeadline_When_GettingActiveOffers_Then_IncludesOffer()
    {
        // Arrange
        var offer = new Offer(
            "Backend Intern",
            "Example Co",
            "C#",
            CurrentUtc.AddMinutes(1),
            2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new GetOffersHandler(
            repository,
            new FixedTimeProvider(CurrentUtc));

        // Act
        var result = handler.Handle(new GetOffersQuery(ActiveOnly: true));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().ContainSingle()
            .Which.Id.Should().Be(offer.Id);
    }

    [Fact]
    public void Given_ClosedOfferWithFutureDeadline_When_GettingActiveOffers_Then_ExcludesOffer()
    {
        // Arrange
        var offer = new Offer(
            "Backend Intern",
            "Example Co",
            "C#",
            CurrentUtc.AddMinutes(1),
            2);
        offer.Close();
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new GetOffersHandler(
            repository,
            new FixedTimeProvider(CurrentUtc));

        // Act
        var result = handler.Handle(new GetOffersQuery(ActiveOnly: true));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_OpenExpiredOffer_When_GettingActiveOffers_Then_ExcludesOffer()
    {
        // Arrange
        var offer = new Offer(
            "Backend Intern",
            "Example Co",
            "C#",
            CurrentUtc.AddMinutes(-1),
            2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new GetOffersHandler(
            repository,
            new FixedTimeProvider(CurrentUtc));

        // Act
        var result = handler.Handle(new GetOffersQuery(ActiveOnly: true));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_OpenOfferWhoseDeadlineEqualsCurrentTime_When_GettingActiveOffers_Then_ExcludesOffer()
    {
        // Arrange
        var offer = new Offer(
            "Backend Intern",
            "Example Co",
            "C#",
            CurrentUtc,
            2);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(offer);
        var handler = new GetOffersHandler(
            repository,
            new FixedTimeProvider(CurrentUtc));

        // Act
        var result = handler.Handle(new GetOffersQuery(ActiveOnly: true));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_OpenClosedAndExpiredOffers_When_GettingAllOffers_Then_ReturnsAllOffers()
    {
        // Arrange
        var openOffer = new Offer(
            "Open Intern",
            "Example Co",
            "C#",
            CurrentUtc.AddMinutes(1),
            2);
        var closedOffer = new Offer(
            "Closed Intern",
            "Example Co",
            "SQL",
            CurrentUtc.AddMinutes(1),
            1);
        closedOffer.Close();
        var expiredOffer = new Offer(
            "Expired Intern",
            "Example Co",
            "Git",
            CurrentUtc.AddMinutes(-1),
            3);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(openOffer);
        repository.Offers.Add(closedOffer);
        repository.Offers.Add(expiredOffer);
        var handler = new GetOffersHandler(
            repository,
            new FixedTimeProvider(CurrentUtc));

        // Act
        var result = handler.Handle(new GetOffersQuery(ActiveOnly: false));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value!.Select(response => response.Id).Should().BeEquivalentTo(
            new[] { openOffer.Id, closedOffer.Id, expiredOffer.Id },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public void Given_NoActiveOffers_When_GettingActiveOffers_Then_ReturnsEmptySuccessList()
    {
        // Arrange
        var closedOffer = new Offer(
            "Closed Intern",
            "Example Co",
            "SQL",
            CurrentUtc.AddMinutes(1),
            1);
        closedOffer.Close();
        var expiredOffer = new Offer(
            "Expired Intern",
            "Example Co",
            "Git",
            CurrentUtc.AddMinutes(-1),
            3);
        var repository = new FakeOfferRepository();
        repository.Offers.Add(closedOffer);
        repository.Offers.Add(expiredOffer);
        var handler = new GetOffersHandler(
            repository,
            new FixedTimeProvider(CurrentUtc));

        // Act
        var result = handler.Handle(new GetOffersQuery(ActiveOnly: true));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
