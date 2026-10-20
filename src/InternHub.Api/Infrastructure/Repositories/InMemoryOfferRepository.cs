using System.Collections.Concurrent;
using InternHub.Api.Domain.Offers;
namespace InternHub.Api.Infrastructure.Repositories;

public class InMemoryOfferRepository : IOfferRepository
{
    private readonly ConcurrentDictionary<Guid, Offer> _offers = new();
    
    public bool Add(Offer offer)
    {
        return _offers.TryAdd(offer.Id, offer);
    }
    
    public Offer? GetById(Guid id)
    {
        return _offers.TryGetValue(id, out var offer)
            ? offer
            : null;
    }

    public IReadOnlyCollection<Offer> GetAll()
    {
        return _offers.Values.ToArray();
    }

    public bool Update(Offer offer)
    {
        if (!_offers.TryGetValue(offer.Id, out var currentOffer))
            return false;

        return _offers.TryUpdate(
            offer.Id,
            offer,
            currentOffer);
    }

    public bool Delete(Guid id)
    {
        return _offers.TryRemove(id, out _);
    }
}