using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

/// <summary>Hand-written stub provider: each call uses the next response (a result or an exception); the last response repeats.</summary>
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
