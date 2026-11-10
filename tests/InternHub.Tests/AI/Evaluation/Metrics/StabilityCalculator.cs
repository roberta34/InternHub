using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed class StabilityCalculator
{
    private readonly SkillNormalizer _normalizer;

    public StabilityCalculator(SkillNormalizer normalizer)
    {
        ArgumentNullException.ThrowIfNull(normalizer);

        _normalizer = normalizer;
    }

    public double Compute(IReadOnlyList<RecruiterEvaluationResult> results, SkillList list)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(list);

        if (results.Count < 2)
            throw new ArgumentException("Stability needs at least two results of the same case.", nameof(results));

        var sets = results.Select(result => _normalizer.ToSet(list.Predicted(result))).ToList();

        return Enumerable.Range(0, sets.Count)
            .SelectMany(first => Enumerable.Range(first + 1, sets.Count - first - 1)
                .Select(second => Jaccard(sets[first], sets[second])))
            .Average();
    }

    public StabilityScores ComputeForCase(IReadOnlyList<RecruiterEvaluationResult> results) =>
        new(Compute(results, SkillList.Matched), Compute(results, SkillList.Missing));

    public StabilityScores MeanOverCases(IReadOnlyList<IReadOnlyList<RecruiterEvaluationResult>> resultsPerCase)
    {
        ArgumentNullException.ThrowIfNull(resultsPerCase);

        if (resultsPerCase.Count == 0)
            throw new ArgumentException("There are no cases to compute stability for.", nameof(resultsPerCase));

        var perCase = resultsPerCase.Select(ComputeForCase).ToList();

        return new StabilityScores(
            perCase.Average(scores => scores.Matched),
            perCase.Average(scores => scores.Missing));
    }

    // Two empty sets have an empty union and count as identical (1).
    private static double Jaccard(HashSet<string> first, HashSet<string> second) =>
        Ratio.Divide(first.Intersect(second).Count(), first.Union(second).Count(), 1);
}
