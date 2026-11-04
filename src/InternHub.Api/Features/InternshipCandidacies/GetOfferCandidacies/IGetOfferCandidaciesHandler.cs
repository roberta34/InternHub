using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;

public interface IGetOfferCandidaciesHandler
{
    Result<IReadOnlyList<GetOfferCandidaciesResponse>> Handle(GetOfferCandidaciesQuery query);
}
