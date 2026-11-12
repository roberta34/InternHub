using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Aggregation;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Aggregation;

/// <summary>Unit tests for MetricAggregator.</summary>
public sealed class MetricAggregatorTests
{
    private static readonly SkillNormalizer Normalizer = new(new Dictionary<string, string>());

    private static MetricAggregator CreateAggregator() =>
        new(CaseMetrics.Default, [SkillList.Matched, SkillList.Missing], Normalizer);

    // Case A: expected {a}, predicted {a}             -> TP 1, FP 0, FN 0; P 1,    R 1,      F1 1
    // Case B: expected {b, c, d}, predicted {b, x, y, z} -> TP 1, FP 3, FN 2; P 0.25, R 0.3333, F1 0.2857
    private static List<(EvaluationCase Case, RecruiterEvaluationResult Result)> TwoCases() =>
    [
        (EvaluationCaseBuilder.With(["a"], []),
            new RecruiterEvaluationResult(["a"], [], [], "Summary A.")),
        (EvaluationCaseBuilder.With(["b", "c", "d"], []),
            new RecruiterEvaluationResult(["b", "x", "y", "z"], [], [], "Summary B."))
    ];

    [Fact]
    public void Given_TwoCases_When_Aggregating_Then_MacroAveragesPerCaseMatchedScores()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var pairs = TwoCases();

        // Act
        var scores = aggregator.Aggregate(pairs);

        // Assert
        scores.Macro["Matched precision"].Should().BeApproximately(0.625, 0.0001);
        scores.Macro["Matched recall"].Should().BeApproximately(0.6667, 0.0001);
        scores.Macro["Matched F1"].Should().BeApproximately(0.6429, 0.0001);
    }

    [Fact]
    public void Given_TwoCases_When_Aggregating_Then_MicroComputesFromSummedMatchedCounts()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var pairs = TwoCases();

        // Act
        var scores = aggregator.Aggregate(pairs);

        // Assert
        var matched = scores.Micro["Matched"];
        matched.Should().Be(new ConfusionCounts(2, 3, 2));
        matched.Precision.Should().BeApproximately(0.4, 0.0001);
        matched.Recall.Should().BeApproximately(0.5, 0.0001);
        matched.F1.Should().BeApproximately(0.4444, 0.0001);
    }

    [Fact]
    public void Given_NoMissingSkillsExpectedOrPredicted_When_Aggregating_Then_MicroMissingScoresAreOne()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var pairs = TwoCases();

        // Act
        var scores = aggregator.Aggregate(pairs);

        // Assert
        var missing = scores.Micro["Missing"];
        missing.Should().Be(ConfusionCounts.Zero);
        missing.Precision.Should().BeApproximately(1, 0.0001);
        missing.Recall.Should().BeApproximately(1, 0.0001);
        missing.F1.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_DefaultMetrics_When_Aggregating_Then_ReportsOneMacroValuePerMetricAndCaseCount()
    {
        // Arrange
        var aggregator = CreateAggregator();
        var pairs = TwoCases();

        // Act
        var scores = aggregator.Aggregate(pairs);

        // Assert
        scores.CaseCount.Should().Be(2);
        scores.Macro.Keys.Should().BeEquivalentTo(
            "Matched precision",
            "Matched recall",
            "Matched F1",
            "Missing precision",
            "Missing recall",
            "Missing F1",
            "Field completeness",
            "Hallucination rate",
            "Contradiction",
            "Coverage");
        scores.Micro.Keys.Should().BeEquivalentTo("Matched", "Missing");
    }

    [Fact]
    public void Given_NoPairs_When_Aggregating_Then_ThrowsArgumentException()
    {
        // Arrange
        var aggregator = CreateAggregator();
        List<(EvaluationCase Case, RecruiterEvaluationResult Result)> pairs = [];

        // Act
        var aggregate = () => aggregator.Aggregate(pairs);

        // Assert
        aggregate.Should().Throw<ArgumentException>()
            .WithParameterName("pairs");
    }
}
