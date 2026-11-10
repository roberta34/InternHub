using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.AI.Evaluation.Running;

public sealed class OutcomeClassifier
{
    // Exact exception types thrown by the inspected providers (KeywordRecruiterEvaluationService,
    // OllamaRecruiterEvaluationService). Configuration errors are excluded by the runner's pre-flight check.
    private static readonly IReadOnlyDictionary<Type, OutcomeKind> ExceptionKinds =
        new Dictionary<Type, OutcomeKind>
        {
            [typeof(InvalidOperationException)] = OutcomeKind.InvalidResponse,
            [typeof(HttpRequestException)] = OutcomeKind.InfrastructureUnavailable,
            [typeof(TaskCanceledException)] = OutcomeKind.InfrastructureUnavailable
        };

    public OutcomeKind ClassifyResult(RecruiterEvaluationResult? result)
    {
        var valid =
            result is not null &&
            result.MatchedSkills is not null &&
            result.MissingSkills is not null &&
            result.Suggestions is not null &&
            !string.IsNullOrWhiteSpace(result.Summary);

        return valid ? OutcomeKind.Success : OutcomeKind.InvalidResponse;
    }

    // Returns false for exception types that are not mapped and for any cancellation requested
    // by the caller's own token, so the caller lets those exceptions propagate unchanged.
    public bool TryClassify(Exception exception, CancellationToken callerToken, out OutcomeKind kind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        kind = default;
        var cancelledByCaller = exception is OperationCanceledException && callerToken.IsCancellationRequested;

        return !cancelledByCaller && ExceptionKinds.TryGetValue(exception.GetType(), out kind);
    }
}
