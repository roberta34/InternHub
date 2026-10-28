using InternHub.Api.Domain.Offers;

namespace InternHub.Tests.Unit.Offers.Fakes;

internal sealed class FakeOfferRepository : IOfferRepository
{
    public List<Offer> Offers { get; } = [];
    public bool AddResult { get; set; } = true;
    public List<Offer> AddedOffers { get; } = [];

    public bool Add(Offer offer)
    {
        if (!AddResult)
            return false;

        AddedOffers.Add(offer);
        Offers.Add(offer);
        return true;
    }

    public Offer? GetById(Guid id) =>
        Offers.SingleOrDefault(offer => offer.Id == id);

    public IReadOnlyCollection<Offer> GetAll() =>
        Offers.ToArray();

    public bool Update(Offer offer) =>
        Offers.Any(existing => existing.Id == offer.Id);

    public bool Delete(Guid id)
    {
        var offer = GetById(id);
        return offer is not null && Offers.Remove(offer);
    }
}
