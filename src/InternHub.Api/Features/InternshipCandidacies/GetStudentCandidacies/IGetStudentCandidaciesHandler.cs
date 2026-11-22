using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public interface IGetStudentCandidaciesHandler
{
    Task<Result<IReadOnlyList<GetStudentCandidaciesResponse>>> HandleAsync(
        GetStudentCandidaciesQuery query,
        CancellationToken cancellationToken = default);
}
