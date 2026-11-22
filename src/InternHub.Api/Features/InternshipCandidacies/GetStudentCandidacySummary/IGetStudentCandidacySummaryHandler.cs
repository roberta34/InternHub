using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public interface IGetStudentCandidacySummaryHandler
{
    Task<Result<GetStudentCandidacySummaryResponse>> HandleAsync(
        GetStudentCandidacySummaryQuery query,
        CancellationToken cancellationToken = default);
}
