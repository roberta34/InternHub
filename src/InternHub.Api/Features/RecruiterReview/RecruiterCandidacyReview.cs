namespace InternHub.Api.Features.RecruiterReview;

public sealed record RecruiterCandidacyReview(
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingOrUnclearSkills,
    IReadOnlyList<SkillEvidence> Evidence,
    string Summary);
