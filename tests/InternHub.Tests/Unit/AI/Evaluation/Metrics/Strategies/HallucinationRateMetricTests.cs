using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Strategies;

/// <summary>Unit tests for HallucinationRateMetric.</summary>
public sealed class HallucinationRateMetricTests
{
    private static readonly SkillNormalizer AliasNormalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    private static readonly SkillNormalizer NoAliasNormalizer =
        new(new Dictionary<string, string>());

    [Fact]
    public void Given_OnlyRequiredSkillsPredicted_When_ComputingHallucination_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], ["docker"]);
        var result = new RecruiterEvaluationResult(["c#", "sql"], ["docker"], ["Learn Docker."], "Summary.");
        var metric = new HallucinationRateMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_TwoOfFourPredictedSkillsNotRequired_When_ComputingHallucination_Then_ReturnsHalf()
    {
        // Arrange
        // Required {c#, sql, docker}; predicted {c#, python, docker, kubernetes}; invented {python, kubernetes} -> 2 / 4
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], ["docker"]);
        var result = new RecruiterEvaluationResult(["c#", "python"], ["docker", "kubernetes"], ["Learn Docker."], "Summary.");
        var metric = new HallucinationRateMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_AllPredictedSkillsInvented_When_ComputingHallucination_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker"]);
        var result = new RecruiterEvaluationResult(["python"], ["kubernetes"], ["Learn Kubernetes."], "Summary.");
        var metric = new HallucinationRateMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Theory]
    [InlineData(new string[] { "c#" }, 0.0)]
    [InlineData(new string[] { }, 0.0)]
    public void Given_EmptyPrediction_When_ComputingHallucination_Then_ReturnsEmptySetValueZero(
        string[] required,
        double expectedValue)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(required, []);
        var result = new RecruiterEvaluationResult([], [], [], "Summary.");
        var metric = new HallucinationRateMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_PredictionWrittenAsAlias_When_ComputingHallucinationWithAliases_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["csharp"], [], [], "Summary.");
        var metric = new HallucinationRateMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_PredictionWrittenAsAlias_When_ComputingHallucinationWithoutAliases_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["csharp"], [], [], "Summary.");
        var metric = new HallucinationRateMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, NoAliasNormalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_Metric_When_ReadingName_Then_ReturnsHallucinationRate()
    {
        // Arrange
        var metric = new HallucinationRateMetric();

        // Act
        var name = metric.Name;

        // Assert
        name.Should().Be("Hallucination rate");
    }
}
