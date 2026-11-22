namespace InternHub.Api.Features.RecruiterReview;

/// <summary>
/// Port for the AI-assisted review a recruiter requests for one candidacy.
/// It only describes the CV against the offer requirements; it never decides.
/// Implementations throw <see cref="RecruiterCandidacyReviewProviderException"/>
/// when the provider cannot produce a review.
/// </summary>
public interface IRecruiterCandidacyReviewService
{
    Task<RecruiterCandidacyReview> ReviewAsync(
        string offerRequirements,
        string cvText,
        CancellationToken cancellationToken = default);
}
