using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public interface IGetStudentCandidacySummaryHandler
{
    Result<GetStudentCandidacySummaryResponse> Handle(GetStudentCandidacySummaryQuery query);
}
