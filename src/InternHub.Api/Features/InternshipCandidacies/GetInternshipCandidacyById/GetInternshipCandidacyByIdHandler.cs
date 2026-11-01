using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;

public sealed class GetInternshipCandidacyByIdHandler(
    IInternshipCandidacyRepository candidacies) : IGetInternshipCandidacyByIdHandler
{
    public Result<GetInternshipCandidacyByIdResponse> Handle(GetInternshipCandidacyByIdQuery query)
    {
        var candidacy = candidacies.GetById(query.Id);

        return candidacy is null
            ? Result<GetInternshipCandidacyByIdResponse>.NotFound(
                $"Internship candidacy {query.Id} was not found.")
            : Result<GetInternshipCandidacyByIdResponse>.Success(
                new GetInternshipCandidacyByIdResponse(
                    candidacy.Id,
                    candidacy.StudentId,
                    candidacy.OfferId,
                    candidacy.ApplicationDate,
                    candidacy.Status));
    }
}
