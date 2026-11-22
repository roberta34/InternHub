using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

public sealed class ContradictionMetric : ICaseMetric
{
    public string Name => "Contradiction";

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(normalizer);

        return normalizer.ToSet(result.MatchedSkills).Overlaps(normalizer.ToSet(result.MissingSkills)) ? 1 : 0;
    }
}
