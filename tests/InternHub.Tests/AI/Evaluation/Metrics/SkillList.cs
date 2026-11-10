using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.AI.Evaluation.Metrics;

public sealed record SkillList(
    string Name,
    Func<RecruiterEvaluationResult, IReadOnlyList<string>> Predicted,
    Func<EvaluationCase, IReadOnlyList<string>> Expected)
{
    public static SkillList Matched { get; } =
        new("Matched", result => result.MatchedSkills, evaluationCase => evaluationCase.ExpectedMatched);

    public static SkillList Missing { get; } =
        new("Missing", result => result.MissingSkills, evaluationCase => evaluationCase.ExpectedMissing);
}
