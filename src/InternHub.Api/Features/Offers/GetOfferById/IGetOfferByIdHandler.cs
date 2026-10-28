using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOfferById;

public interface IGetOfferByIdHandler
{
    Result<GetOfferByIdResponse> Handle(GetOfferByIdQuery query);
}
