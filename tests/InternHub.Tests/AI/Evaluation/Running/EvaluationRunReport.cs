using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.AI.Evaluation.Running;

public sealed record EvaluationRunReport(
    EvaluationRunContext Context,
    int TotalCalls,
    IReadOnlyDictionary<OutcomeKind, int> OutcomeCounts,
    double? ValidResponseRate,
    double UnavailabilityRate,
    bool StoppedAfterFirstUnavailableCall,
    AggregateScores? Aggregate,
    IReadOnlyDictionary<EvaluationCategory, AggregateScores> ByCategory,
    StabilityScores? Stability,
    int StabilityCaseCount,
    IReadOnlyList<ProviderRunOutcome> Outcomes);
