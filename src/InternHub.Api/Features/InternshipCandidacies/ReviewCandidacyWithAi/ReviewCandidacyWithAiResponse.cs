namespace InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;

public sealed record ReviewCandidacyWithAiResponse(
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingOrUnclearSkills,
    IReadOnlyList<ReviewCandidacyWithAiEvidence> Evidence,
    string Summary);

public sealed record ReviewCandidacyWithAiEvidence(string Skill, string Excerpt);
