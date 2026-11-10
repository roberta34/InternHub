using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed class FieldCompletenessMetric : ICaseMetric
{
    public string Name => "Field completeness";

    public double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(result);

        var listsPresent =
            result.MatchedSkills is not null &&
            result.MissingSkills is not null &&
            result.Suggestions is not null;

        var complete =
            listsPresent &&
            !string.IsNullOrWhiteSpace(result.Summary) &&
            (result.MissingSkills!.Count == 0 || result.Suggestions!.Count > 0);

        return complete ? 1 : 0;
    }
}
