using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public sealed class GetStudentCandidaciesHandler(
    IInternshipCandidacyRepository candidacies,
    IStudentRepository students,
    IOfferRepository offers,
    IStudentAccessGuard studentAccessGuard) : IGetStudentCandidaciesHandler
{
    public async Task<Result<IReadOnlyList<GetStudentCandidaciesResponse>>> HandleAsync(
        GetStudentCandidaciesQuery query,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var access = await studentAccessGuard.AuthorizeStudentAccessAsync(
            query.User,
            query.StudentId,
            cancellationToken);

        if (access.Status == StudentAccessStatus.Unauthorized)
            return Result<IReadOnlyList<GetStudentCandidaciesResponse>>.Unauthorized(
                access.Error ?? "Authentication is required.");

        if (access.Status == StudentAccessStatus.Forbidden)
            return Result<IReadOnlyList<GetStudentCandidaciesResponse>>.Forbidden(
                access.Error ?? "Access to student candidacies is forbidden.");

        cancellationToken.ThrowIfCancellationRequested();

        if (students.GetById(query.StudentId) is null)
            return Result<IReadOnlyList<GetStudentCandidaciesResponse>>.NotFound(
                $"Student {query.StudentId} not found.");

        var response = candidacies.GetByStudent(query.StudentId)
            .OrderByDescending(candidacy => candidacy.ApplicationDate)
            .ThenBy(candidacy => candidacy.Id)
            .Select(candidacy =>
            {
                var offer = offers.GetById(candidacy.OfferId)
                    ?? throw new InvalidOperationException(
                        $"Offer {candidacy.OfferId} referenced by candidacy {candidacy.Id} was not found.");

                return new GetStudentCandidaciesResponse(
                    candidacy.Id,
                    candidacy.OfferId,
                    offer.Title,
                    offer.CompanyName,
                    candidacy.ApplicationDate,
                    candidacy.Status);
            })
            .ToList();

        return Result<IReadOnlyList<GetStudentCandidaciesResponse>>.Success(response);
    }
}
