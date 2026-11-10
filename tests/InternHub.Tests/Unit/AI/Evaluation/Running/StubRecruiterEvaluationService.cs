using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.Unit.AI.Evaluation.Running;

// Hand-written stub: each call uses the next response; the last response repeats.
// A response either returns a fixed result or throws a given exception.
internal sealed class StubRecruiterEvaluationService(params Func<RecruiterEvaluationResult>[] responses)
    : IRecruiterEvaluationService
{
    public int CallCount { get; private set; }

    public Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        var response = responses[Math.Min(CallCount, responses.Length - 1)];
        CallCount++;

        return Task.FromResult(response());
    }
}
