using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

/// <summary>Registry: the only list of per-case metrics the evaluator computes, in report order.</summary>
public static class CaseMetrics
{
    public static IReadOnlyList<ICaseMetric> Default { get; } =
    [
        ConfusionMetric.Precision(SkillList.Matched),
        ConfusionMetric.Recall(SkillList.Matched),
        ConfusionMetric.F1(SkillList.Matched),
        ConfusionMetric.Precision(SkillList.Missing),
        ConfusionMetric.Recall(SkillList.Missing),
        ConfusionMetric.F1(SkillList.Missing),
        new FieldCompletenessMetric(),
        new HallucinationRateMetric(),
        new ContradictionMetric(),
        new CoverageMetric()
    ];
}
