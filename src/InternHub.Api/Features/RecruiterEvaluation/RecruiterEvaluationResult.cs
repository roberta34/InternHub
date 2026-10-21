namespace InternHub.Api.Features.RecruiterEvaluation;

public sealed record RecruiterEvaluationResult(
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> Suggestions,
    string Summary);