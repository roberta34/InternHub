namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed record AggregateScores(
    int CaseCount,
    IReadOnlyDictionary<string, double> Macro,
    IReadOnlyDictionary<string, MicroScores> Micro);
