using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CreateOffer;

public interface ICreateOfferHandler
{
    Result<CreateOfferResponse> Handle(CreateOfferCommand command);
}
