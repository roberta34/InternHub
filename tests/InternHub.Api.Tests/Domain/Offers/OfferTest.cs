using InternHub.Api.Domain.Offers;

namespace InternHub.Api.Tests.Domain.Offers;

public class OfferTests
{
    [Fact]
    public void Constructor_WithValidData_CreatesOffer()
    {
        // Arrange
        var deadline = DateTime.UtcNow.AddDays(30);

        // Act
        var offer = new Offer(
            "Software Engineer Intern",
            "Microsoft",
            "C#, .NET, Git",
            deadline,
            3);

        // Assert
        Assert.NotEqual(Guid.Empty, offer.Id);
        Assert.Equal("Software Engineer Intern", offer.Title);
        Assert.Equal("Microsoft", offer.CompanyName);
        Assert.Equal("C#, .NET, Git", offer.Requirements);
        Assert.Equal(deadline, offer.Deadline);
        Assert.Equal(3, offer.MaximumAvailableSlots);
        Assert.False(offer.IsClosed);
        Assert.Equal(0, offer.Version);
    }

    [Fact]
    public void Constructor_WithEmptyTitle_ThrowsArgumentException()
    {
        // Act
        var action = () => new Offer(
            "",
            "Microsoft",
            "C#, .NET",
            DateTime.UtcNow.AddDays(30),
            3);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithEmptyCompanyName_ThrowsArgumentException()
    {
        // Act
        var action = () => new Offer(
            "Software Engineer Intern",
            "",
            "C#, .NET",
            DateTime.UtcNow.AddDays(30),
            3);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }

    [Fact]
    public void Constructor_WithEmptyRequirements_ThrowsArgumentException()
    {
        // Act
        var action = () => new Offer(
            "Software Engineer Intern",
            "Microsoft",
            "",
            DateTime.UtcNow.AddDays(30),
            3);

        // Assert
        Assert.Throws<ArgumentException>(action);
    }
}