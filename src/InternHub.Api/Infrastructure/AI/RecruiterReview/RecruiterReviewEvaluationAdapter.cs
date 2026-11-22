using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Features.RecruiterReview;
using InternHub.Api.Features.StudentCvFeedback;

namespace InternHub.Api.Infrastructure.AI.RecruiterReview;

/// <summary>
/// Adapter: exposes the configured <see cref="IRecruiterEvaluationService"/> (Baseline, Ollama,
/// fallback) as a recruiter candidacy review. Evidence is never invented: a matched skill keeps
/// its place only when it really occurs in the CV.
/// </summary>
public sealed class RecruiterReviewEvaluationAdapter(
    IRecruiterEvaluationService evaluator) : IRecruiterCandidacyReviewService
{
    private const string UnavailableMessage = "The AI review provider could not produce a review.";

    public async Task<RecruiterCandidacyReview> ReviewAsync(
        string offerRequirements,
        string cvText,
        CancellationToken cancellationToken = default)
    {
        RecruiterEvaluationResult evaluation;

        try
        {
            evaluation = await evaluator.AnalyzeAsync(cvText, offerRequirements, cancellationToken);
        }
        catch (StudentCvFeedbackProviderException exception)
        {
            throw new RecruiterCandidacyReviewProviderException(UnavailableMessage, exception);
        }
        catch (HttpRequestException exception)
        {
            throw new RecruiterCandidacyReviewProviderException(UnavailableMessage, exception);
        }
        // Ollama reports invalid JSON after its retry this way, consistent with OllamaFallbackDecorator.
        catch (InvalidOperationException exception)
        {
            throw new RecruiterCandidacyReviewProviderException(UnavailableMessage, exception);
        }
        catch (OperationCanceledException exception)
            when (!cancellationToken.IsCancellationRequested)
        {
            throw new RecruiterCandidacyReviewProviderException(UnavailableMessage, exception);
        }

        if (evaluation?.MatchedSkills is null || evaluation.MissingSkills is null)
            throw new RecruiterCandidacyReviewProviderException(UnavailableMessage);

        var excerpts = SkillExcerptLocator.Locate(cvText, evaluation.MatchedSkills);
        var reportedMatched = Distinct(evaluation.MatchedSkills);
        var matched = reportedMatched.Where(excerpts.ContainsKey).ToList();
        var missingOrUnclear = Distinct(
                reportedMatched.Where(skill => !excerpts.ContainsKey(skill)).Concat(evaluation.MissingSkills))
            .Where(skill => !matched.Contains(skill, StringComparer.OrdinalIgnoreCase))
            .ToList();
        var evidence = matched.Select(skill => new SkillEvidence(skill, excerpts[skill])).ToList();

        return new RecruiterCandidacyReview(
            matched,
            missingOrUnclear,
            evidence,
            evaluation.Summary ?? string.Empty);
    }

    private static List<string> Distinct(IEnumerable<string> skills) =>
        skills
            .Where(skill => !string.IsNullOrWhiteSpace(skill))
            .Select(skill => skill.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
}
