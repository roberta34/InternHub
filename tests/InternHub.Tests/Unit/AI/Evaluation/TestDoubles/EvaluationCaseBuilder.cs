using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

/// <summary>Builds a minimal EvaluationCase from expected matched and missing skills.</summary>
internal static class EvaluationCaseBuilder
{
    public static EvaluationCase With(IReadOnlyList<string> expectedMatched, IReadOnlyList<string> expectedMissing) =>
        new(
            "clear-01",
            EvaluationCategory.Clear,
            "Fictional CV text.",
            "Fictional requirements text.",
            [.. expectedMatched, .. expectedMissing],
            expectedMatched,
            expectedMissing,
            "Builder case for metric tests.");
}
