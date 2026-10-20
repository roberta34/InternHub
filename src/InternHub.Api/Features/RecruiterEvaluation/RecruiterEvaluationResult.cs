namespace InternHub.Api.Features.RecruiterEvaluation;

public sealed record RecruiterEvaluationResult(
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingOrUnclearSkills,
    IReadOnlyList<string> Evidence,
    string Summary);