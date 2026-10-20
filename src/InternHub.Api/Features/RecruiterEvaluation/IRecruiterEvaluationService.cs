
using InternHub.Api.Features.RecruiterEvaluation;
namespace InternHub.Api.Features.RecruiterEvaluation;

public interface IRecruiterEvaluationService
{
    Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default);
}