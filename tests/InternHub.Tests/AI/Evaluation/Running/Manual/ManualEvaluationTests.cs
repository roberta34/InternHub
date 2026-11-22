using FluentAssertions;
using InternHub.Api.Infrastructure.AI.Baseline;
using InternHub.Api.Infrastructure.AI.Ollama;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Running;
using InternHub.Tests.AI.Evaluation.Running.Outcomes;
using InternHub.Tests.AI.Evaluation.Running.Reporting;
using Microsoft.Extensions.Configuration;
using Xunit.Abstractions;

namespace InternHub.Tests.AI.Evaluation.Running.Manual;

public sealed class ManualEvaluationTests(ITestOutputHelper output)
{
    private static readonly TimeSpan OllamaTimeout = TimeSpan.FromSeconds(120);

    [ManualEvaluationFact]
    public async Task Given_KeywordBaseline_When_RunningManualEvaluation_Then_WritesReport()
    {
        // Arrange
        var settings = ManualEvaluationSettings.FromEnvironment();
        var context = BaselineContext(settings);
        var service = new KeywordRecruiterEvaluationService();
        var runner = new EvaluationRunner(new OutcomeClassifier());

        // Act
        var report = await runner.RunAsync(
            EvaluationDatasetLoader.Load(),
            service,
            new Dictionary<string, string?>(),
            context);
        var path = WriteReport(report);

        // Assert
        File.Exists(path).Should().BeTrue();
    }

    [ManualEvaluationFact]
    public async Task Given_OllamaProvider_When_RunningManualEvaluation_Then_WritesReport()
    {
        // Arrange
        var settings = ManualEvaluationSettings.FromEnvironment();
        var requiredSettings = OllamaSettings(settings);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(requiredSettings).Build();
        using var httpClient = new HttpClient { Timeout = OllamaTimeout };
        var service = new OllamaRecruiterEvaluationService(httpClient, configuration);
        var runner = new EvaluationRunner(new OutcomeClassifier());

        // Act
        var report = await runner.RunAsync(
            EvaluationDatasetLoader.Load(),
            service,
            requiredSettings,
            OllamaContext(settings));
        var path = WriteReport(report);

        // Assert
        File.Exists(path).Should().BeTrue();
    }

    private static EvaluationRunContext BaselineContext(ManualEvaluationSettings settings) =>
        new("keyword-baseline", "n/a", "n/a", "n/a", 1, settings.RunDate, settings.Commit);

    private static EvaluationRunContext OllamaContext(ManualEvaluationSettings settings) =>
        new(
            "ollama",
            settings.OllamaModel ?? ManualEvaluationSettings.NotSet,
            settings.Temperature,
            settings.PromptVersion,
            settings.RunsPerCase,
            settings.RunDate,
            settings.Commit);

    private static Dictionary<string, string?> OllamaSettings(ManualEvaluationSettings settings) =>
        new()
        {
            ["Ai:Ollama:Endpoint"] = settings.OllamaEndpoint,
            ["Ai:Ollama:Model"] = settings.OllamaModel
        };

    private string WriteReport(EvaluationRunReport report)
    {
        var markdown = MarkdownReportWriter.Write(report);
        var path = Path.Combine(AppContext.BaseDirectory, MarkdownReportWriter.FileName(report.Context));
        File.WriteAllText(path, markdown);
        output.WriteLine(markdown);
        output.WriteLine($"Report written to {path}");

        return path;
    }
}
