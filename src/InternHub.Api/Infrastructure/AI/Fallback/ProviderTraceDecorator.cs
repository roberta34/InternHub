using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Infrastructure.AI.Baseline;
using InternHub.Api.Infrastructure.AI.Ollama;

namespace InternHub.Api.Infrastructure.AI.Fallback;

public sealed class ProviderTraceDecorator(
    IRecruiterEvaluationService inner,
    AiExecutionTrace trace) : IRecruiterEvaluationService
{
    public async Task<RecruiterEvaluationResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default)
    {
        trace.Provider = GetProvider(inner);
        trace.FallbackUsed = false;

        return await inner.AnalyzeAsync(cvContent, offerRequirements, cancellationToken);
    }

    private static string GetProvider(IRecruiterEvaluationService service) =>
        service switch
        {
            KeywordRecruiterEvaluationService => "Baseline",
            OllamaRecruiterEvaluationService => "Ollama",
            _ => throw new InvalidOperationException(
                "The recruiter evaluation provider is not supported.")
        };
}
