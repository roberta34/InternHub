using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.AI.Evaluation.Metrics.Strategies;

/// <summary>Strategy: computes one named score for one case and one provider result.</summary>
public interface ICaseMetric
{
    string Name { get; }

    double Compute(EvaluationCase evaluationCase, RecruiterEvaluationResult result, SkillNormalizer normalizer);
}
