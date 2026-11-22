using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Strategies;

/// <summary>Unit tests for CoverageMetric.</summary>
public sealed class CoverageMetricTests
{
    private static readonly SkillNormalizer Normalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    [Fact]
    public void Given_EveryRequiredSkillPredicted_When_ComputingCoverage_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], ["docker"]);
        var result = new RecruiterEvaluationResult(["csharp", "SQL"], ["docker"], ["Learn Docker."], "Summary.");
        var metric = new CoverageMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_TwoOfThreeRequiredSkillsPredicted_When_ComputingCoverage_Then_ReturnsTwoThirds()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], ["docker"]);
        var result = new RecruiterEvaluationResult(["c#", "python"], ["docker", "kubernetes"], ["Learn Docker."], "Summary.");
        var metric = new CoverageMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0.6667, 0.0001);
    }

    [Theory]
    [InlineData(new string[] { "c#" }, 0.0)]
    [InlineData(new string[] { }, 1.0)]
    public void Given_EmptyPrediction_When_ComputingCoverage_Then_FollowsEmptySetConvention(
        string[] required,
        double expectedValue)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(required, []);
        var result = new RecruiterEvaluationResult([], [], [], "Summary.");
        var metric = new CoverageMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_RequiredSkillInBothLists_When_ComputingCoverage_Then_CountsItOnce()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult(["c#"], ["c#"], ["Practice C#."], "Summary.");
        var metric = new CoverageMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_Metric_When_ReadingName_Then_ReturnsCoverage()
    {
        // Arrange
        var metric = new CoverageMetric();

        // Act
        var name = metric.Name;

        // Assert
        name.Should().Be("Coverage");
    }
}
