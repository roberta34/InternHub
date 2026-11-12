using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Aggregation;

/// <summary>Value Object: case count, macro value per metric and summed ConfusionCounts per skill list (micro).</summary>
public sealed record AggregateScores(
    int CaseCount,
    IReadOnlyDictionary<string, double> Macro,
    IReadOnlyDictionary<string, ConfusionCounts> Micro);
