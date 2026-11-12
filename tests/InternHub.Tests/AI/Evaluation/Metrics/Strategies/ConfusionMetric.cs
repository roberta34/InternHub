using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

public sealed class ConfusionMetric : ICaseMetric
{
    private readonly SkillList _list;
    private readonly Func<ConfusionCounts, double> _score;

    private ConfusionMetric(SkillList list, string scoreName, Func<ConfusionCounts, double> score)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _score = score;
        Name = $"{list.Name} {scoreName}";
    }

    public string Name { get; }

    public static ConfusionMetric Precision(SkillList list) => new(list, "precision", counts => counts.Precision);

    public static ConfusionMetric Recall(SkillList list) => new(list, "recall", counts => counts.Recall);

    public static ConfusionMetric F1(SkillList list) => new(list, "F1", counts => counts.F1);

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer) =>
        _score(ConfusionCounts.For(evaluationCase, result, normalizer, _list));
}
