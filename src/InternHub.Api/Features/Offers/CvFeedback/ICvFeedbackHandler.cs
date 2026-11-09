using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Offers.CvFeedback;

public interface ICvFeedbackHandler
{
    Task<Result<CvFeedbackResponse>> HandleAsync(
        CvFeedbackCommand command,
        CancellationToken cancellationToken);
}
