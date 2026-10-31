using FluentAssertions;
using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;

namespace InternHub.Tests.Unit.Offers;

public sealed class OfferTests
{
    private static readonly DateTime Deadline =
        new(2027, 1, 15, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Given_ValidOfferData_When_CreatingOffer_Then_InitializesOfferState()
    {
        // Arrange

        // Act
        var offer = new Offer("Software Engineer Intern", "Microsoft", "C#, .NET, Git", Deadline, 3);

        // Assert
        offer.Id.Should().NotBe(Guid.Empty);
        offer.Title.Should().Be("Software Engineer Intern");
        offer.CompanyName.Should().Be("Microsoft");
        offer.Requirements.Should().Be("C#, .NET, Git");
        offer.Deadline.Should().Be(Deadline);
        offer.MaximumAvailableSlots.Should().Be(3);
        offer.IsClosed.Should().BeFalse();
        offer.Version.Should().Be(0);
        offer.InternshipCandidacies.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Given_InvalidTitle_When_CreatingOffer_Then_ThrowsArgumentException(string? title)
    {
        // Arrange

        // Act
        var action = () => new Offer(title!, "Microsoft", "C#, .NET", Deadline, 3);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Title is required.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Given_InvalidCompanyName_When_CreatingOffer_Then_ThrowsArgumentException(string? companyName)
    {
        // Arrange

        // Act
        var action = () => new Offer("Software Engineer Intern", companyName!, "C#, .NET", Deadline, 3);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Company name is required.*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Given_InvalidRequirements_When_CreatingOffer_Then_ThrowsArgumentException(string? requirements)
    {
        // Arrange

        // Act
        var action = () => new Offer("Software Engineer Intern", "Microsoft", requirements!, Deadline, 3);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Requirements are required.*");
    }

    [Fact]
    public void Given_ValidOfferData_When_UpdatingOffer_Then_UpdatesValuesAndIncrementsVersion()
    {
        // Arrange
        var offer = new Offer("Old title", "Old company", "Old requirements", Deadline, 1);
        var updatedDeadline = new DateTime(2027, 2, 20, 0, 0, 0, DateTimeKind.Utc);

        // Act
        offer.Update("New title", "New company", "New requirements", updatedDeadline, 4);

        // Assert
        offer.Title.Should().Be("New title");
        offer.CompanyName.Should().Be("New company");
        offer.Requirements.Should().Be("New requirements");
        offer.Deadline.Should().Be(updatedDeadline);
        offer.MaximumAvailableSlots.Should().Be(4);
        offer.Version.Should().Be(1);
    }

    [Fact]
    public void Given_OpenOffer_When_ClosingOffer_Then_SetsIsClosedToTrue()
    {
        // Arrange
        var offer = new Offer("Title", "Company", "Requirements", Deadline, 1);

        // Act
        offer.Close();

        // Assert
        offer.IsClosed.Should().BeTrue();
    }

    [Fact]
    public void Given_OpenOffer_When_ClosingOffer_Then_IncrementsVersionExactlyOnce()
    {
        // Arrange
        var offer = new Offer("Title", "Company", "Requirements", Deadline, 1);

        // Act
        offer.Close();

        // Assert
        offer.Version.Should().Be(1);
    }

    [Fact]
    public void Given_ClosedOffer_When_ClosingOfferAgain_Then_PreservesClosedStateAndVersion()
    {
        // Arrange
        var offer = new Offer("Title", "Company", "Requirements", Deadline, 1);
        offer.Close();
        var versionAfterClosing = offer.Version;

        // Act
        offer.Close();

        // Assert
        offer.IsClosed.Should().BeTrue();
        offer.Version.Should().Be(versionAfterClosing);
    }

    [Fact]
    public void Given_OpenOffer_When_ClosingOffer_Then_PreservesOtherProperties()
    {
        // Arrange
        var offer = new Offer("Title", "Company", "Requirements", Deadline, 1);
        var id = offer.Id;
        var title = offer.Title;
        var companyName = offer.CompanyName;
        var requirements = offer.Requirements;
        var deadline = offer.Deadline;
        var maximumAvailableSlots = offer.MaximumAvailableSlots;
        var internshipCandidacies = offer.InternshipCandidacies.ToArray();

        // Act
        offer.Close();

        // Assert
        offer.Id.Should().Be(id);
        offer.Title.Should().Be(title);
        offer.CompanyName.Should().Be(companyName);
        offer.Requirements.Should().Be(requirements);
        offer.Deadline.Should().Be(deadline);
        offer.MaximumAvailableSlots.Should().Be(maximumAvailableSlots);
        offer.InternshipCandidacies.Should().BeEquivalentTo(internshipCandidacies);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Given_InvalidTitle_When_UpdatingOffer_Then_PreservesExistingState(string? title)
    {
        // Arrange
        var offer = new Offer("Old title", "Old company", "Old requirements", Deadline, 1);

        // Act
        var action = () => offer.Update(title!, "New company", "New requirements", Deadline, 4);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Title is required.*");
        offer.Title.Should().Be("Old title");
        offer.Version.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Given_InvalidCompanyName_When_UpdatingOffer_Then_PreservesExistingState(string? companyName)
    {
        // Arrange
        var offer = new Offer("Old title", "Old company", "Old requirements", Deadline, 1);

        // Act
        var action = () => offer.Update("New title", companyName!, "New requirements", Deadline, 4);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Company name is required.*");
        offer.CompanyName.Should().Be("Old company");
        offer.Version.Should().Be(0);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Given_InvalidRequirements_When_UpdatingOffer_Then_PreservesExistingState(string? requirements)
    {
        // Arrange
        var offer = new Offer("Old title", "Old company", "Old requirements", Deadline, 1);

        // Act
        var action = () => offer.Update("New title", "New company", requirements!, Deadline, 4);

        // Assert
        action.Should().Throw<ArgumentException>().WithMessage("*Requirements are required.*");
        offer.Requirements.Should().Be("Old requirements");
        offer.Version.Should().Be(0);
    }

    [Fact]
    public void Given_Offer_When_ReadingInternshipCandidacies_Then_ReturnsReadOnlyCollection()
    {
        // Arrange
        var offer = new Offer("Title", "Company", "Requirements", Deadline, 1);

        // Act
        var collection = offer.InternshipCandidacies;

        // Assert
        collection.Should().BeAssignableTo<ICollection<InternshipCandidacy>>();
        ((ICollection<InternshipCandidacy>)collection).IsReadOnly.Should().BeTrue();
    }
}
