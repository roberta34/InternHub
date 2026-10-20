using InternHub.Api.Domain.Offers;
using InternHub.Api.Infrastructure.Repositories;

namespace InternHub.Tests.Unit.Repositories;
public class InMemoryOfferRepositoryTests
{
    private static Offer CreateOffer()
    {
        return new Offer(
            "Software Engineer Intern",
            "Microsoft",
            "C#, .NET, Git",
            DateTime.UtcNow.AddDays(30),
            3);
    }

    [Fact]
    public void Add_NewOffer_ReturnsTrue()
    {
        var repository = new InMemoryOfferRepository();
        var offer = CreateOffer();

        var result = repository.Add(offer);

        Assert.True(result);
    }

    [Fact]
    public void Add_DuplicateOffer_ReturnsFalse()
    {
        var repository = new InMemoryOfferRepository();
        var offer = CreateOffer();

        repository.Add(offer);

        var result = repository.Add(offer);

        Assert.False(result);
    }

    [Fact]
    public void GetById_ExistingOffer_ReturnsOffer()
    {
        var repository = new InMemoryOfferRepository();
        var offer = CreateOffer();

        repository.Add(offer);

        var result = repository.GetById(offer.Id);

        Assert.NotNull(result);
        Assert.Equal(offer.Id, result.Id);
    }

    [Fact]
    public void GetById_MissingOffer_ReturnsNull()
    {
        var repository = new InMemoryOfferRepository();

        var result = repository.GetById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsAllOffers()
    {
        var repository = new InMemoryOfferRepository();

        repository.Add(CreateOffer());
        repository.Add(CreateOffer());

        var result = repository.GetAll();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetAll_ReturnsSnapshot()
    {
        var repository = new InMemoryOfferRepository();

        var firstOffer = CreateOffer();
        repository.Add(firstOffer);

        var snapshot = repository.GetAll();

        repository.Add(CreateOffer());

        Assert.Single(snapshot);
        Assert.Equal(2, repository.GetAll().Count);
    }

    [Fact]
    public void Update_ExistingOffer_ReturnsTrue()
    {
        var repository = new InMemoryOfferRepository();
        var offer = CreateOffer();

        repository.Add(offer);

        offer.Update(
            "Updated Software Engineer Intern",
            "Microsoft",
            "C#, .NET, Git, Azure",
            DateTime.UtcNow.AddDays(60),
            5);

        var result = repository.Update(offer);

        Assert.True(result);
    }

    [Fact]
    public void Update_MissingOffer_ReturnsFalse()
    {
        var repository = new InMemoryOfferRepository();
        var offer = CreateOffer();

        var result = repository.Update(offer);

        Assert.False(result);
    }

    [Fact]
    public void Delete_ExistingOffer_ReturnsTrue()
    {
        var repository = new InMemoryOfferRepository();
        var offer = CreateOffer();

        repository.Add(offer);

        var result = repository.Delete(offer.Id);

        Assert.True(result);
        Assert.Null(repository.GetById(offer.Id));
    }

    [Fact]
    public void Delete_MissingOffer_ReturnsFalse()
    {
        var repository = new InMemoryOfferRepository();

        var result = repository.Delete(Guid.NewGuid());

        Assert.False(result);
    }
}