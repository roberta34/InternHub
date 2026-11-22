namespace InternHub.Api.Features.StudentCvFeedback;

public sealed record StudentCvFeedbackResult(
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> Suggestions,
    string Summary);