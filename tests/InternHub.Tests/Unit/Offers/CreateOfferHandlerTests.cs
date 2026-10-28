using FluentAssertions;
using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Offers.Fakes;

namespace InternHub.Tests.Unit.Offers;

public sealed class CreateOfferHandlerTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ValidCommand_When_CreatingOffer_Then_ReturnsOfferAndPersistsIt()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new CreateOfferHandler(repository);
        var command = new CreateOfferCommand("Backend Intern", "Example Co", "C#", Deadline, 2);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Backend Intern");
        result.Value.CompanyName.Should().Be("Example Co");
        result.Value.Requirements.Should().Be("C#");
        result.Value.Deadline.Should().Be(Deadline);
        result.Value.MaximumAvailableSlots.Should().Be(2);
        result.Value.IsClosed.Should().BeFalse();
        result.Value.Version.Should().Be(0);
        repository.AddedOffers.Should().ContainSingle();
        repository.AddedOffers[0].Id.Should().Be(result.Value.Id);
    }

    [Fact]
    public void Given_InvalidCommand_When_CreatingOffer_Then_ReturnsValidationWithoutPersisting()
    {
        // Arrange
        var repository = new FakeOfferRepository();
        var handler = new CreateOfferHandler(repository);
        var command = new CreateOfferCommand("", "Example Co", "C#", Deadline, 2);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("Title is required.");
        repository.AddedOffers.Should().BeEmpty();
    }

    [Fact]
    public void Given_RepositoryConflict_When_CreatingOffer_Then_ReturnsConflict()
    {
        // Arrange
        var repository = new FakeOfferRepository { AddResult = false };
        var handler = new CreateOfferHandler(repository);
        var command = new CreateOfferCommand("Backend Intern", "Example Co", "C#", Deadline, 2);

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
        result.Value.Should().BeNull();
    }
}
