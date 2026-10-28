using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.GetOffers;

public interface IGetOffersHandler
{
    Result<IReadOnlyList<GetOffersResponse>> Handle(GetOffersQuery query);
}
