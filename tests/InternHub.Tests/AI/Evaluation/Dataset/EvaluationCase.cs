namespace InternHub.Tests.AI.Evaluation.Dataset;

/// <summary>Value Object: one labeled dataset case (CV, requirements, required skills, expected matched and missing skills).</summary>
public sealed record EvaluationCase(
    string Id,
    EvaluationCategory Category,
    string CvText,
    string RequirementsText,
    IReadOnlyList<string> RequiredSkills,
    IReadOnlyList<string> ExpectedMatched,
    IReadOnlyList<string> ExpectedMissing,
    string Rationale);
