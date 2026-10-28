using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.UpdateOffer;

public interface IUpdateOfferHandler
{
    Result<bool> Handle(UpdateOfferCommand command);
}
