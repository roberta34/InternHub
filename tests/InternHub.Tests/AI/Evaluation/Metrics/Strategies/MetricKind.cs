namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

/// <summary>Whether a metric checks the shape of the output (Structural) or compares it with the dataset labels (Semantic).</summary>
public enum MetricKind
{
    Structural,
    Semantic
}
