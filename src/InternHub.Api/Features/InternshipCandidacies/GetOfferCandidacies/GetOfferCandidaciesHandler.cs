using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;

public sealed class GetOfferCandidaciesHandler(
    IInternshipCandidacyRepository candidacies,
    IOfferRepository offers,
    IStudentRepository students) : IGetOfferCandidaciesHandler
{
    public Result<IReadOnlyList<GetOfferCandidaciesResponse>> Handle(GetOfferCandidaciesQuery query)
    {
        if (offers.GetById(query.OfferId) is null)
            return Result<IReadOnlyList<GetOfferCandidaciesResponse>>.NotFound(
                $"Offer {query.OfferId} not found.");

        var response = candidacies.GetByOffer(query.OfferId)
            .OrderByDescending(candidacy => candidacy.ApplicationDate)
            .ThenBy(candidacy => candidacy.Id)
            .Select(candidacy =>
            {
                var student = students.GetById(candidacy.StudentId)
                    ?? throw new InvalidOperationException(
                        $"Student {candidacy.StudentId} referenced by candidacy {candidacy.Id} was not found.");

                return new GetOfferCandidaciesResponse(
                    candidacy.Id,
                    candidacy.StudentId,
                    student.FullName,
                    candidacy.ApplicationDate,
                    candidacy.Status);
            })
            .ToList();

        return Result<IReadOnlyList<GetOfferCandidaciesResponse>>.Success(response);
    }
}
