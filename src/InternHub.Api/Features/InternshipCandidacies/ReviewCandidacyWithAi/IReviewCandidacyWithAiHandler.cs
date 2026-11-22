using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;

public interface IReviewCandidacyWithAiHandler
{
    Task<Result<ReviewCandidacyWithAiResponse>> HandleAsync(
        ReviewCandidacyWithAiCommand command,
        CancellationToken cancellationToken = default);
}
