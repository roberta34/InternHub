using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.DeleteOffer;

public interface IDeleteOfferHandler
{
    Result<bool> Handle(DeleteOfferCommand command);
}
