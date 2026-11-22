using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.StudentCvFeedback;

namespace InternHub.Api.Features.Offers.CvFeedback;

public sealed class CvFeedbackHandler(
    IOfferRepository offers,
    IRecruiterEvaluationService evaluator) : ICvFeedbackHandler
{
    public const int MaximumCvLength = 20_000;

    public async Task<Result<CvFeedbackResponse>> HandleAsync(
        CvFeedbackCommand command,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(command.CvText))
        {
            return Result<CvFeedbackResponse>.Validation(
                "CV text must not be empty.");
        }

        if (command.CvText.Length > MaximumCvLength)
        {
            return Result<CvFeedbackResponse>.Validation(
                $"CV text must not exceed {MaximumCvLength} characters.");
        }

        var offer = offers.GetById(command.OfferId);
        if (offer is null)
        {
            return Result<CvFeedbackResponse>.NotFound(
                $"Offer {command.OfferId} not found.");
        }

        RecruiterEvaluationResult evaluation;
        try
        {
            evaluation = await evaluator.AnalyzeAsync(
                command.CvText,
                offer.Requirements,
                cancellationToken);
        }
        
        catch (StudentCvFeedbackProviderException exception)
            when (exception.Kind is
                StudentCvFeedbackFailureKind.ProviderUnavailable
                or StudentCvFeedbackFailureKind.Timeout
                or StudentCvFeedbackFailureKind.InvalidEvaluation)
        {
            return ProviderUnavailable();
        }

        return Result<CvFeedbackResponse>.Success(
            new CvFeedbackResponse(
                evaluation.MatchedSkills,
                evaluation.MissingSkills,
                evaluation.Suggestions,
                evaluation.Summary));
    }

    private static Result<CvFeedbackResponse> ProviderUnavailable() =>
        Result<CvFeedbackResponse>.ServiceUnavailable(
            "CV feedback is temporarily unavailable.");
}
