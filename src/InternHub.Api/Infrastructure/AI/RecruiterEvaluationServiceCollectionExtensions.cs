using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Infrastructure.AI.Baseline;
using InternHub.Api.Infrastructure.AI.Fallback;
using InternHub.Api.Infrastructure.AI.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace InternHub.Api.Infrastructure.AI;

public static class RecruiterEvaluationServiceCollectionExtensions
{
    public static IServiceCollection AddRecruiterEvaluation(
        this IServiceCollection services)
    {
        services.AddOptions<AiOptions>()
            .BindConfiguration("Ai")
            .Validate(
                options => IsSupportedProvider(options.Provider),
                "Configuration 'Ai:Provider' must be 'Baseline' or 'Ollama'.")
            .Validate(
                options => !IsOllamaProvider(options.Provider)
                    || Uri.TryCreate(options.Ollama.Endpoint, UriKind.Absolute, out _),
                "Configuration 'Ai:Ollama:Endpoint' must be an absolute URI when 'Ai:Provider' is 'Ollama'.")
            .Validate(
                options => !IsOllamaProvider(options.Provider)
                    || !string.IsNullOrWhiteSpace(options.Ollama.Model),
                "Configuration 'Ai:Ollama:Model' must be configured when 'Ai:Provider' is 'Ollama'.")
            .ValidateOnStart();

        services.AddScoped<KeywordRecruiterEvaluationService>();
        services.AddHttpClient<OllamaRecruiterEvaluationService>();
        services.AddScoped<AiExecutionTrace>();
        services.AddScoped<IRecruiterEvaluationService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<AiOptions>>().Value;
            var baseline = provider.GetRequiredService<KeywordRecruiterEvaluationService>();
            var selectedProvider = options.Provider?.Trim().ToUpperInvariant();

            return selectedProvider switch
            {
                "BASELINE" => new ProviderTraceDecorator(
                    baseline,
                    provider.GetRequiredService<AiExecutionTrace>()),
                "OLLAMA" when options.Fallback.Enabled => new OllamaFallbackDecorator(
                    provider.GetRequiredService<OllamaRecruiterEvaluationService>(),
                    baseline,
                    provider.GetRequiredService<AiExecutionTrace>()),
                "OLLAMA" => new ProviderTraceDecorator(
                    provider.GetRequiredService<OllamaRecruiterEvaluationService>(),
                    provider.GetRequiredService<AiExecutionTrace>()),
                _ => throw new InvalidOperationException(
                    "Configuration 'Ai:Provider' must be 'Baseline' or 'Ollama'.")
            };
        });

        return services;
    }

    private static bool IsSupportedProvider(string? provider) =>
        string.Equals(provider?.Trim(), "Baseline", StringComparison.OrdinalIgnoreCase)
        || string.Equals(provider?.Trim(), "Ollama", StringComparison.OrdinalIgnoreCase);

    private static bool IsOllamaProvider(string? provider) =>
        string.Equals(provider?.Trim(), "Ollama", StringComparison.OrdinalIgnoreCase);
}
