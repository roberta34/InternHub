using InternHub.Api.Domain.Offers;

namespace InternHub.Tests.Unit.Offers.Fakes;

internal sealed class FakeOfferRepository : IOfferRepository
{
    public List<Offer> Offers { get; } = [];
    public bool AddResult { get; set; } = true;
    public List<Offer> AddedOffers { get; } = [];
    public int UpdateCallCount { get; private set; }
    public int DeleteCallCount { get; private set; }
    public Exception? UpdateException { get; set; }
    public bool UpdateResult { get; set; } = true;

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

    public bool Update(Offer offer)
    {
        UpdateCallCount++;

        if (UpdateException is not null)
            throw UpdateException;

        return UpdateResult && Offers.Any(existing => existing.Id == offer.Id);
    }

    public bool Delete(Guid id)
    {
        DeleteCallCount++;
        var offer = GetById(id);
        return offer is not null && Offers.Remove(offer);
    }
}
