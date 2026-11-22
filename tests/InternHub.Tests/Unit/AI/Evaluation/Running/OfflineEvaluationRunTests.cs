using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Running;
using InternHub.Tests.AI.Evaluation.Running.Outcomes;
using InternHub.Tests.AI.Evaluation.Running.Reporting;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Running;

/// <summary>
/// Offline end-to-end run of the evaluator over the whole dataset with ScriptedEvaluationProvider (no real provider).
/// Every expected value is computed by hand from the dataset labels and the three documented perturbations:
/// negation-03 (negated docker moved to matched), suggestion-mismatch-01 (extra unrelated suggestion),
/// minimal-01 (blank summary -> InvalidResponse, excluded from every aggregate).
/// </summary>
public sealed class OfflineEvaluationRunTests
{
    private static readonly EvaluationRunContext Context =
        new("scripted", "offline", "n/a", "n/a", 1, "offline", "offline");

    private static readonly IReadOnlyDictionary<string, string?> NoSettings = new Dictionary<string, string?>();

    private static Task<EvaluationRunReport> RunAsync()
    {
        var dataset = EvaluationDatasetLoader.Load();
        return new EvaluationRunner(new OutcomeClassifier())
            .RunAsync(dataset, new ScriptedEvaluationProvider(dataset), NoSettings, Context);
    }

    [Fact]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_CountsOutcomesAndValidResponseRate()
    {
        // Arrange
        // 26 cases, K = 1 -> 26 calls. Only minimal-01 (blank summary) is InvalidResponse -> 25 successes.
        // Valid-response rate = successes / calls that reached the model = 25 / (25 + 1) = 0.9615.

        // Act
        var report = await RunAsync();

        // Assert
        report.TotalCalls.Should().Be(26);
        report.OutcomeCounts[OutcomeKind.Success].Should().Be(25);
        report.OutcomeCounts[OutcomeKind.InvalidResponse].Should().Be(1);
        report.OutcomeCounts[OutcomeKind.InfrastructureUnavailable].Should().Be(0);
        report.ValidResponseRate.Should().BeApproximately(25.0 / 26, 0.0001);
        report.Aggregate!.CaseCount.Should().Be(25);
    }

    [Fact]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_MicroMatchedHasOneFalsePositive()
    {
        // Arrange
        // Matched TP = |expectedMatched| summed over the 25 successful cases:
        //   clear-01 3, clear-03 3, synonyms-01 3, synonyms-02 3, negation-01 2, negation-02 1, implicit-01 1,
        //   implicit-02 1, noise-01 2, noise-02 1, prompt-injection-01 1, negation-03 2, negation-04 2, noise-03 1,
        //   prompt-injection-03 1, prompt-injection-04 2, ambiguous-terms-01 1, ambiguous-terms-02 1,
        //   suggestion-mismatch-01 1, suggestion-mismatch-02 1, unsupported-evidence-01 1, unsupported-evidence-02 1
        //   (clear-02, minimal-02, prompt-injection-02 have 0) = 35.
        // Matched FP = 1 (negation-03 reports docker as matched); FN = 0.
        // Micro matched precision = 35 / 36 = 0.9722; recall = 35 / 35 = 1.

        // Act
        var report = await RunAsync();

        // Assert
        var matched = report.Aggregate!.Micro["Matched"];
        matched.Precision.Should().BeApproximately(35.0 / 36, 0.0001);
        matched.Recall.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_MicroMissingHasOneFalseNegative()
    {
        // Arrange
        // Expected missing summed over the 25 successful cases:
        //   clear-02 3, clear-03 1, synonyms-02 1, negation-01 1, negation-02 2, implicit-01 1, implicit-02 1,
        //   noise-01 1, noise-02 2, minimal-02 3, prompt-injection-01 2, prompt-injection-02 3, negation-03 1,
        //   negation-04 1, noise-03 2, prompt-injection-03 2, prompt-injection-04 2, ambiguous-terms-01 2,
        //   ambiguous-terms-02 1, suggestion-mismatch-01 1, suggestion-mismatch-02 1, unsupported-evidence-01 1,
        //   unsupported-evidence-02 1 (clear-01, synonyms-01 have 0) = 36.
        // negation-03 reports no missing skill -> TP 36 - 1 = 35, FN 1, FP 0.
        // Micro missing precision = 35 / 35 = 1; recall = 35 / 36 = 0.9722.

        // Act
        var report = await RunAsync();

        // Assert
        var missing = report.Aggregate!.Micro["Missing"];
        missing.Precision.Should().BeApproximately(1, 0.0001);
        missing.Recall.Should().BeApproximately(35.0 / 36, 0.0001);
    }

    [Fact]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_MacroScoresDifferFromMicroOnlyThroughNegation03()
    {
        // Arrange
        // Every successful case except negation-03 has per-case matched precision 1 and missing recall 1
        // (empty-set convention: 1 when prediction and labels are both empty).
        // negation-03: matched precision 2 / 3, missing recall 0 / 1.
        // Macro matched precision = (24 + 2/3) / 25 = 0.9867; macro missing recall = (24 + 0) / 25 = 0.96.

        // Act
        var report = await RunAsync();

        // Assert
        report.Aggregate!.Macro["Matched precision"].Should().BeApproximately((24 + 2.0 / 3) / 25, 0.0001);
        report.Aggregate.Macro["Missing recall"].Should().BeApproximately(24.0 / 25, 0.0001);
    }

    [Fact]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_SuggestionScoresReflectOnlySuggestionMismatch01()
    {
        // Arrange
        // Coverage: every reported missing skill has its own "Practice <skill> ..." suggestion in all 25 cases
        //   (negation-03 reports none -> 1 by convention) -> macro 25 / 25 = 1.
        // Mismatch: suggestion-mismatch-01 has 2 suggestions, 1 naming no skill -> 1 / 2 = 0.5;
        //   the other 24 cases score 0 (cases without suggestions -> 0 by convention) -> macro 0.5 / 25 = 0.02.

        // Act
        var report = await RunAsync();

        // Assert
        report.Aggregate!.Macro["Suggestion coverage"].Should().BeApproximately(1, 0.0001);
        report.Aggregate.Macro["Suggestion mismatch"].Should().BeApproximately(0.5 / 25, 0.0001);
    }

    [Fact]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_NoHallucinationContradictionOrIncompleteField()
    {
        // Arrange
        // The scripted provider only returns dataset labels: every predicted skill is in requiredSkills
        //   (negation-03's docker is required) -> hallucination 0 in all 25 cases -> macro 0.
        // negation-03 moves docker instead of duplicating it -> no skill in both lists -> contradiction macro 0.
        // All 25 successes have non-null lists, a summary, and a suggestion whenever a skill is missing
        //   (negation-03 has no missing skill) -> field completeness macro 1.

        // Act
        var report = await RunAsync();

        // Assert
        report.Aggregate!.Macro["Hallucination rate"].Should().BeApproximately(0, 0.0001);
        report.Aggregate.Macro["Contradiction"].Should().BeApproximately(0, 0.0001);
        report.Aggregate.Macro["Field completeness"].Should().BeApproximately(1, 0.0001);
    }

    [Theory]
    [InlineData(EvaluationCategory.Minimal, 1)]
    [InlineData(EvaluationCategory.AmbiguousTerms, 2)]
    [InlineData(EvaluationCategory.SuggestionMismatch, 2)]
    [InlineData(EvaluationCategory.UnsupportedEvidence, 2)]
    public async Task Given_ScriptedProvider_When_RunningWholeDataset_Then_BreaksDownSuccessesPerCategory(
        EvaluationCategory category,
        int expectedResults)
    {
        // Arrange
        // Minimal: minimal-01 is excluded (InvalidResponse), only minimal-02 remains -> 1.
        // New categories: both cases of each succeed -> 2.

        // Act
        var report = await RunAsync();

        // Assert
        report.ByCategory[category].CaseCount.Should().Be(expectedResults);
    }

    [Fact]
    public async Task Given_ScriptedRunReport_When_Writing_Then_ContainsNoCvSuggestionOrSummaryText()
    {
        // Arrange
        var dataset = EvaluationDatasetLoader.Load();
        var report = await RunAsync();
        var providerTexts = dataset.Cases
            .SelectMany(evaluationCase => evaluationCase.ExpectedMissing)
            .Select(ScriptedEvaluationProvider.SuggestionFor)
            .Append(ScriptedEvaluationProvider.UnrelatedSuggestion)
            .Append(ScriptedEvaluationProvider.Summary);

        // Act
        var markdown = MarkdownReportWriter.Write(report);

        // Assert
        markdown.Should().NotContainAny(dataset.Cases.Select(evaluationCase => evaluationCase.CvText));
        markdown.Should().NotContainAny(providerTexts);
        markdown.Should().Contain("## Structural metrics").And.Contain("## Semantic metrics");
    }
}
