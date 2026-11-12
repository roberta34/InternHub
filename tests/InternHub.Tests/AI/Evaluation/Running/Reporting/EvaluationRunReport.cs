using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Aggregation;
using InternHub.Tests.AI.Evaluation.Metrics.Stability;
using InternHub.Tests.AI.Evaluation.Running.Outcomes;

namespace InternHub.Tests.AI.Evaluation.Running.Reporting;

/// <summary>Value Object: everything one run measured, ready to be written as a report.</summary>
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
