using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.Unit.RecruiterReview.Fakes;

/// <summary>Stub provider: returns a fixed evaluation or throws a given exception.</summary>
internal sealed class StubRecruiterEvaluationService : IRecruiterEvaluationService
{
    public RecruiterEvaluationResult Result { get; set; } =
        new([], [], [], "Stub evaluation.");

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
