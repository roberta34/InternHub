using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed class RecallMetric(SkillList list) : ICaseMetric
{
    private readonly SkillList _list = list ?? throw new ArgumentNullException(nameof(list));

    public string Name => $"{_list.Name} recall";

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer) =>
        ConfusionCounts.For(evaluationCase, result, normalizer, _list).Recall;
}
