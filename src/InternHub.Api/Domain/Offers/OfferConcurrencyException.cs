namespace InternHub.Api.Domain.Offers;

public sealed class OfferConcurrencyException : Exception
{
    public OfferConcurrencyException(Guid offerId, Exception innerException)
        : base($"Offer {offerId} was modified concurrently.", innerException)
    {
    }
}
