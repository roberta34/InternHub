using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;

namespace InternHub.Tests.AI.Evaluation.Metrics.Aggregation;

/// <summary>Aggregates the registered metrics over all cases: macro means and micro summed ConfusionCounts.</summary>
public sealed class MetricAggregator
{
    private readonly IReadOnlyList<ICaseMetric> _metrics;
    private readonly IReadOnlyList<SkillList> _lists;
    private readonly SkillNormalizer _normalizer;

    public MetricAggregator(
        IReadOnlyList<ICaseMetric> metrics,
        IReadOnlyList<SkillList> lists,
        SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        ArgumentNullException.ThrowIfNull(lists);
        ArgumentNullException.ThrowIfNull(normalizer);

        _metrics = metrics;
        _lists = lists;
        _normalizer = normalizer;
    }

    public AggregateScores Aggregate(
        IReadOnlyList<(EvaluationCase Case, RecruiterEvaluationResult Result)> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        if (pairs.Count == 0)
            throw new ArgumentException("There are no successful outcomes to aggregate.", nameof(pairs));

        var macro = _metrics.ToDictionary(
            metric => metric.Name,
            metric => pairs.Average(pair => metric.Compute(pair.Case, pair.Result, _normalizer)));

        var micro = _lists.ToDictionary(
            list => list.Name,
            list => pairs
                .Select(pair => ConfusionCounts.For(pair.Case, pair.Result, _normalizer, list))
                .Aggregate(ConfusionCounts.Zero, (total, counts) => total + counts));

        return new AggregateScores(pairs.Count, macro, micro);
    }
}
