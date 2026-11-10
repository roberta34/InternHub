namespace InternHub.Tests.AI.Evaluation.Metrics;

public static class CaseMetrics
{
    public static IReadOnlyList<ICaseMetric> Default { get; } =
    [
        new PrecisionMetric(SkillList.Matched),
        new RecallMetric(SkillList.Matched),
        new F1Metric(SkillList.Matched),
        new PrecisionMetric(SkillList.Missing),
        new RecallMetric(SkillList.Missing),
        new F1Metric(SkillList.Missing),
        new FieldCompletenessMetric(),
        new HallucinationRateMetric(),
        new ContradictionMetric(),
        new CoverageMetric()
    ];
}
