using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

public sealed class CoverageMetric : ICaseMetric
{
    public string Name => "Coverage";

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(evaluationCase);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(normalizer);

        var required = normalizer.ToSet(evaluationCase.RequiredSkills);
        var predicted = normalizer.ToSet(result.MatchedSkills.Concat(result.MissingSkills));
        var covered = required.Count(predicted.Contains);

        return Ratio.Divide(covered, required.Count, 1);
    }
}
