namespace InternHub.Api.Features.RecruiterReview;

/// <summary>A matched skill and the short CV fragment where it was found.</summary>
public sealed record SkillEvidence(string Skill, string Excerpt);
