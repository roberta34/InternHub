using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;
using InternHub.Api.Features.Offers.CvFeedback;
using InternHub.Api.Features.RecruiterReview;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;

/// <summary>
/// Read-only AI review of one candidacy for a recruiter. It never changes the candidacy,
/// never saves anything and never logs or stores the CV text.
/// </summary>
public sealed class ReviewCandidacyWithAiHandler(
    IInternshipCandidacyRepository candidacies,
    IOfferRepository offers,
    IRecruiterCandidacyReviewService reviewer,
    IRecruiterAccessGuard recruiterAccess) : IReviewCandidacyWithAiHandler
{
    public const int MaximumCvLength = CvFeedbackHandler.MaximumCvLength;

    public async Task<Result<ReviewCandidacyWithAiResponse>> HandleAsync(
        ReviewCandidacyWithAiCommand command,
        CancellationToken cancellationToken = default)
    {
        var access = await recruiterAccess.ResolveAuthenticatedRecruiterAsync(command.User, cancellationToken);
        if (!access.IsAuthorized)
            return access.ToFailure<ReviewCandidacyWithAiResponse>();

        if (string.IsNullOrWhiteSpace(command.CvText))
            return Result<ReviewCandidacyWithAiResponse>.Validation("CV text must not be empty.");

        if (command.CvText.Length > MaximumCvLength)
            return Result<ReviewCandidacyWithAiResponse>.Validation(
                $"CV text must not exceed {MaximumCvLength} characters.");

        var candidacy = candidacies.GetById(command.CandidacyId);
        if (candidacy is null)
            return Result<ReviewCandidacyWithAiResponse>.NotFound(
                $"Candidacy {command.CandidacyId} not found.");

        var offer = offers.GetById(candidacy.OfferId);
        if (offer is null)
            return Result<ReviewCandidacyWithAiResponse>.NotFound(
                $"Offer of candidacy {command.CandidacyId} not found.");

        if (offer.RecruiterId != access.RecruiterId)
            return Result<ReviewCandidacyWithAiResponse>.Forbidden(
                $"Only the owning Recruiter can review candidacy {command.CandidacyId}.");

        RecruiterCandidacyReview review;
        try
        {
            review = await reviewer.ReviewAsync(offer.Requirements, command.CvText, cancellationToken);
        }
        catch (RecruiterCandidacyReviewProviderException)
        {
            return Result<ReviewCandidacyWithAiResponse>.ServiceUnavailable(
                "AI review is temporarily unavailable.");
        }

        return Result<ReviewCandidacyWithAiResponse>.Success(
            new ReviewCandidacyWithAiResponse(
                review.MatchedSkills,
                review.MissingOrUnclearSkills,
                review.Evidence
                    .Select(item => new ReviewCandidacyWithAiEvidence(item.Skill, item.Excerpt))
                    .ToList(),
                review.Summary));
    }
}
