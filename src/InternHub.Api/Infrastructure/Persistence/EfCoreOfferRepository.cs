using InternHub.Api.Domain.Offers;
using Microsoft.EntityFrameworkCore;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class EfCoreOfferRepository(InternHubDbContext db) : IOfferRepository
{
    public bool Add(Offer offer)
    {
        db.Offers.Add(offer);
        db.SaveChanges();
        return true;
    }

    public Offer? GetById(Guid id)
    {
        return db.Offers
            .Include(o => o.InternshipCandidacies)
            .FirstOrDefault(o => o.Id == id);
    }

    public IReadOnlyCollection<Offer> GetAll()
    {
        return db.Offers
            .Include(o => o.InternshipCandidacies)
            .ToList();
    }

    public bool Update(Offer offer)
    {
        var exists = db.Offers.Any(o => o.Id == offer.Id);
        if (!exists)
            return false;

        db.Offers.Update(offer);

        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new OfferConcurrencyException(offer.Id, exception);
        }

        return true;
    }

    public bool Delete(Guid id)
    {
        var offer = db.Offers.FirstOrDefault(o => o.Id == id);
        if (offer is null)
            return false;

        db.Offers.Remove(offer);
        db.SaveChanges();
        return true;
    }
}
