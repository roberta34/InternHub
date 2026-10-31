using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CloseOffer;

public interface ICloseOfferHandler
{
    Result<bool> Handle(CloseOfferCommand command);
}