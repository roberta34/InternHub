using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.AI.Evaluation.Metrics.Core;

/// <summary>Value Object: TP, FP and FN of one skill list, with precision, recall and F1 derived from them.</summary>
public sealed record ConfusionCounts(int TruePositives, int FalsePositives, int FalseNegatives)
{
    public static ConfusionCounts Zero { get; } = new(0, 0, 0);

    // Denominator TP + FP = |pred|; when it is 0, precision is 1 only if exp is also empty (FN = 0).
    public double Precision =>
        Ratio.Divide(TruePositives, TruePositives + FalsePositives, FalseNegatives == 0 ? 1 : 0);

    // Denominator TP + FN = |exp|; when it is 0, recall is 1 only if pred is also empty (FP = 0).
    public double Recall =>
        Ratio.Divide(TruePositives, TruePositives + FalseNegatives, FalsePositives == 0 ? 1 : 0);

    public double F1 => Ratio.Divide(2 * Precision * Recall, Precision + Recall, 0);

    public static ConfusionCounts For(
        EvaluationCase evaluationCase,
        RecruiterEvaluationResult result,
        SkillNormalizer normalizer,
        SkillList list)
    {
        ArgumentNullException.ThrowIfNull(evaluationCase);
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(normalizer);
        ArgumentNullException.ThrowIfNull(list);

        var predicted = normalizer.ToSet(list.Predicted(result));
        var expected = normalizer.ToSet(list.Expected(evaluationCase));
        var truePositives = predicted.Count(expected.Contains);

        return new ConfusionCounts(
            truePositives,
            predicted.Count - truePositives,
            expected.Count - truePositives);
    }

    public static ConfusionCounts operator +(ConfusionCounts left, ConfusionCounts right) =>
        new(
            left.TruePositives + right.TruePositives,
            left.FalsePositives + right.FalsePositives,
            left.FalseNegatives + right.FalseNegatives);
}
