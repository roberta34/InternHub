using InternHub.Api.Features.RecruiterEvaluation;

namespace InternHub.Api.Infrastructure.AI.Fallback;

public sealed class OllamaFallbackDecorator(
    IRecruiterEvaluationService primary,
    IRecruiterEvaluationService fallback,
    AiExecutionTrace trace) : IRecruiterEvaluationService
{
    public async Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await primary.AnalyzeAsync(
                cvContent,
                offerRequirements,
                cancellationToken);

            trace.Provider = "Ollama";
            trace.FallbackUsed = false;
            return result;
        }
        catch (Exception exception) when (IsEligible(exception, cancellationToken))
        {
            trace.Provider = "Baseline";
            trace.FallbackUsed = true;
            return await fallback.AnalyzeAsync(
                cvContent,
                offerRequirements,
                cancellationToken);
        }
    }

    private static bool IsEligible(Exception exception, CancellationToken cancellationToken) =>
        !cancellationToken.IsCancellationRequested
        && exception is HttpRequestException
            or OperationCanceledException
            or InvalidOperationException;
}
