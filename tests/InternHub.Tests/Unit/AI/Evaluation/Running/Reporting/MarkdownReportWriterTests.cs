using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Running;
using InternHub.Tests.AI.Evaluation.Running.Outcomes;
using InternHub.Tests.AI.Evaluation.Running.Reporting;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Running.Reporting;

/// <summary>Unit tests for MarkdownReportWriter.</summary>
public sealed class MarkdownReportWriterTests
{
    private static readonly IReadOnlyDictionary<string, string?> NoSettings = new Dictionary<string, string?>();

    private static readonly EvaluationRunContext Context =
        new("stub-provider", "stub-model", "0.1", "prompt-v-test", 2, "2026-01-01", "abc1234");

    // Expected matched {c#}, missing {docker}. Predicted matched {c#, skillmarker}, missing {docker}.
    // Matched: TP 1, FP 1, FN 0 per run -> precision 0.5 (micro and macro), recall 1.
    private static EvaluationDataset MarkerDataset() =>
        new(
            "test",
            true,
            new Dictionary<string, string>(),
            [
                EvaluationCaseBuilder.With(["c#"], ["docker"]) with
                {
                    Id = "clear-01",
                    CvText = "CV-MARKER-7f3a",
                    RequirementsText = "REQ-MARKER-7f3a"
                }
            ]);

    private static readonly RecruiterEvaluationResult MarkerResult =
        new(["c#", "SKILL-MARKER-7f3a"], ["docker"], ["SUGGESTION-MARKER-7f3a"], "SUMMARY-MARKER-7f3a");

    private static Task<EvaluationRunReport> RunAsync(StubRecruiterEvaluationService service) =>
        new EvaluationRunner(new OutcomeClassifier()).RunAsync(MarkerDataset(), service, NoSettings, Context);

    [Fact]
    public async Task Given_Report_When_Writing_Then_ContainsEveryContextValue()
    {
        // Arrange
        var report = await RunAsync(new StubRecruiterEvaluationService(() => MarkerResult));

        // Act
        var markdown = MarkdownReportWriter.Write(report);

        // Assert
        markdown.Should().ContainAll(
            "| Provider | stub-provider |",
            "| Model | stub-model |",
            "| Temperature | 0.1 |",
            "| Prompt version | prompt-v-test |",
            "| Runs per case (K) | 2 |",
            "| Date | 2026-01-01 |",
            "| Commit | abc1234 |");
    }

    [Fact]
    public async Task Given_Report_When_Writing_Then_ContainsMetricsCategoriesAndStability()
    {
        // Arrange
        var report = await RunAsync(new StubRecruiterEvaluationService(() => MarkerResult));

        // Act
        var markdown = MarkdownReportWriter.Write(report);

        // Assert
        markdown.Should().ContainAll(
            "| Matched precision | 0.5000 | 0.5000 |",
            "| Matched recall | 1.0000 | 1.0000 |",
            "| Hallucination rate |",
            "| Contradiction |",
            "| Coverage |",
            "| Field completeness |",
            "| Clear | 2 |",
            "| Matched | 1.0000 |",
            "| Success | 2 |");
    }

    [Fact]
    public async Task Given_Report_When_Writing_Then_StatesThatProviderRetriesAreInvisible()
    {
        // Arrange
        var report = await RunAsync(new StubRecruiterEvaluationService(() => MarkerResult));

        // Act
        var markdown = MarkdownReportWriter.Write(report);

        // Assert
        markdown.Should().Contain("Provider-internal retries are invisible to the evaluator");
    }

    [Fact]
    public async Task Given_CvAndGeneratedTextWithMarkers_When_Writing_Then_ContainsNoneOfThem()
    {
        // Arrange
        var report = await RunAsync(new StubRecruiterEvaluationService(() => MarkerResult));

        // Act
        var markdown = MarkdownReportWriter.Write(report);

        // Assert
        markdown.Should().NotContainAny("MARKER", "marker");
    }

    [Fact]
    public async Task Given_RunStoppedAfterFirstUnavailableCall_When_Writing_Then_ShowsStopNotice()
    {
        // Arrange
        var report = await RunAsync(new StubRecruiterEvaluationService(() => throw new HttpRequestException("Down.")));

        // Act
        var markdown = MarkdownReportWriter.Write(report);

        // Assert
        markdown.Should().ContainAll(
            "**Run stopped:**",
            "No successful outcomes; metrics were not computed.",
            "| clear-01 | Clear | 1 | InfrastructureUnavailable |");
    }

    [Theory]
    [InlineData("ollama", "llama3.2:3b", "ai-evaluation-ollama-llama3.2-3b.md")]
    [InlineData("keyword-baseline", "n/a", "ai-evaluation-keyword-baseline-n-a.md")]
    [InlineData("Ollama", "Qwen2.5 7B", "ai-evaluation-ollama-qwen2.5-7b.md")]
    public void Given_ProviderAndModel_When_BuildingFileName_Then_ReturnsSanitizedName(
        string provider,
        string model,
        string expected)
    {
        // Arrange
        var context = new EvaluationRunContext(provider, model, "0.1", "v1", 1, "2026-01-01", "abc1234");

        // Act
        var fileName = MarkdownReportWriter.FileName(context);

        // Assert
        fileName.Should().Be(expected);
    }
}
