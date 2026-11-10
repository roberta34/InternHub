namespace InternHub.Tests.AI.Evaluation.Dataset;

public sealed record EvaluationCase(
    string Id,
    EvaluationCategory Category,
    string CvText,
    string RequirementsText,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> ExpectedMatched,
    IReadOnlyList<string> ExpectedMissing,
    string Rationale);
