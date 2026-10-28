using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.DeleteOffer;

public sealed class DeleteOfferHandler(IOfferRepository offers) : IDeleteOfferHandler
{
    public Result<bool> Handle(DeleteOfferCommand command)
    {
        var deleted = offers.Delete(command.Id);

        return deleted
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound($"Offer {command.Id} not found.");
    }
}
