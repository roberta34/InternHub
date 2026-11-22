using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

/// <summary>Strategy: share of predicted skills that the case does not require.</summary>
public sealed class HallucinationRateMetric : ICaseMetric
{
    public string Name => "Hallucination rate";

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(evaluationCase);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(normalizer);

        var required = normalizer.ToSet(evaluationCase.RequiredSkills);
        var predicted = normalizer.ToSet(result.MatchedSkills.Concat(result.MissingSkills));
        var invented = predicted.Count(skill => !required.Contains(skill));

        return Ratio.Divide(invented, predicted.Count, 0);
    }
}
