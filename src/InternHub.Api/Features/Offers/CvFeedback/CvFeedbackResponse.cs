namespace InternHub.Api.Features.Offers.CvFeedback;

public sealed record CvFeedbackResponse(
    IReadOnlyList<string> MatchedSkills,
    IReadOnlyList<string> MissingSkills,
    IReadOnlyList<string> Suggestions,
    string Summary);
