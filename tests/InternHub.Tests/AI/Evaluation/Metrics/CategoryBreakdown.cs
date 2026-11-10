using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed class CategoryBreakdown
{
    private readonly MetricAggregator _aggregator;

    public CategoryBreakdown(MetricAggregator aggregator)
    {
        ArgumentNullException.ThrowIfNull(aggregator);

        _aggregator = aggregator;
    }

    public IReadOnlyDictionary<EvaluationCategory, AggregateScores> Aggregate(
        IReadOnlyList<(EvaluationCase Case, RecruiterEvaluationResult Result)> pairs)
    {
        ArgumentNullException.ThrowIfNull(pairs);

        return pairs
            .GroupBy(pair => pair.Case.Category)
            .ToDictionary(group => group.Key, group => _aggregator.Aggregate(group.ToList()));
    }
}
