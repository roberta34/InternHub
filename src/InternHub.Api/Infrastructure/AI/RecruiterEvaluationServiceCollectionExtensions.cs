using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Infrastructure.AI.Baseline;
using InternHub.Api.Infrastructure.AI.Ollama;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Infrastructure.AI;

public static class RecruiterEvaluationServiceCollectionExtensions
{
    public static IServiceCollection AddRecruiterEvaluation(
        this IServiceCollection services)
    {
        services.AddScoped<KeywordRecruiterEvaluationService>();
        services.AddHttpClient<OllamaRecruiterEvaluationService>();
        services.AddScoped<IRecruiterEvaluationService>(provider =>
        {
            var configuration = provider.GetRequiredService<IConfiguration>();
            var selectedProvider = configuration["Ai:Provider"]?.Trim();

            return selectedProvider?.ToUpperInvariant() switch
            {
                "BASELINE" => provider.GetRequiredService<KeywordRecruiterEvaluationService>(),
                "OLLAMA" => provider.GetRequiredService<OllamaRecruiterEvaluationService>(),
                _ => throw new InvalidOperationException(
                    "Configuration 'Ai:Provider' must be 'Baseline' or 'Ollama'.")
            };
        });

        return services;
    }
}
