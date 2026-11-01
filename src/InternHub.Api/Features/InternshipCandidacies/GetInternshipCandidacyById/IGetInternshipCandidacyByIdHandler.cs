using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;

public interface IGetInternshipCandidacyByIdHandler
{
    Result<GetInternshipCandidacyByIdResponse> Handle(GetInternshipCandidacyByIdQuery query);
}
