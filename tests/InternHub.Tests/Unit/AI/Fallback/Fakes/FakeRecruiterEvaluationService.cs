using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.Unit.AI.Fallback.Fakes;

internal sealed class FakeRecruiterEvaluationService : IRecruiterEvaluationService
{
    public RecruiterEvaluationResult Result { get; set; } =
        new([], [], [], "Fake evaluation.");

    public Exception? ExceptionToThrow { get; set; }
    public int CallCount { get; private set; }

    public Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        CallCount++;

        var exception = ExceptionToThrow;
        return exception is null
            ? Task.FromResult(Result)
            : Task.FromException<RecruiterEvaluationResult>(exception);
    }
}
