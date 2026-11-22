namespace InternHub.Api.Features.RecruiterReview;

/// <summary>
/// The review provider was unavailable, timed out or returned an unusable review.
/// The message must never contain CV text.
/// </summary>
public sealed class RecruiterCandidacyReviewProviderException : Exception
{
    public RecruiterCandidacyReviewProviderException(
        string message,
        Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
