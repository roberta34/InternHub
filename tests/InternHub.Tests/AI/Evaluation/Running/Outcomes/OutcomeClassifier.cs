using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.AI.Evaluation.Running.Outcomes;

public sealed class OutcomeClassifier
{
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
    
    public bool TryClassify(Exception exception, CancellationToken callerToken, out OutcomeKind kind)
    {
        ArgumentNullException.ThrowIfNull(exception);

        kind = default;
        var cancelledByCaller = exception is OperationCanceledException && callerToken.IsCancellationRequested;

        return !cancelledByCaller && ExceptionKinds.TryGetValue(exception.GetType(), out kind);
    }
}
