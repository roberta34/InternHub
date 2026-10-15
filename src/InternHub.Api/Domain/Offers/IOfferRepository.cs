namespace InternHub.Api.Domain.Offers;

public interface IOfferRepository
{
    bool Add(Offer offer);

    Offer? GetById(Guid id);

    IReadOnlyCollection<Offer> GetAll();

    bool Update(Offer offer);

    bool Delete(Guid id);
}