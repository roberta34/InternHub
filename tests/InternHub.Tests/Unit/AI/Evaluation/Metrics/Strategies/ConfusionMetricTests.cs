using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Strategies;

/// <summary>Unit tests for ConfusionMetric.</summary>
public sealed class ConfusionMetricTests
{
    private static readonly SkillNormalizer AliasNormalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    private static readonly SkillNormalizer NoAliasNormalizer =
        new(new Dictionary<string, string>());

    private static readonly IReadOnlyDictionary<string, Func<SkillList, ConfusionMetric>> Factories =
        new Dictionary<string, Func<SkillList, ConfusionMetric>>
        {
            ["Precision"] = ConfusionMetric.Precision,
            ["Recall"] = ConfusionMetric.Recall,
            ["F1"] = ConfusionMetric.F1
        };

    private static readonly IReadOnlyDictionary<string, SkillList> Lists =
        new Dictionary<string, SkillList>
        {
            ["Matched"] = SkillList.Matched,
            ["Missing"] = SkillList.Missing
        };

    [Fact]
    public void Given_PerfectMatchWithDifferentCase_When_ComputingMatchedPrecision_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult(["C#", "SQL"], [], [], "Summary.");
        var metric = ConfusionMetric.Precision(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_DisjointSets_When_ComputingMatchedPrecision_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["java"], [], [], "Summary.");
        var metric = ConfusionMetric.Precision(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_PartialOverlap_When_ComputingMatchedPrecision_Then_ReturnsHalf()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql", "docker"], []);
        var result = new RecruiterEvaluationResult(["c#", "java"], [], [], "Summary.");
        var metric = ConfusionMetric.Precision(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { }, 1.0)]
    [InlineData(new string[] { }, new string[] { "docker" }, 0.0)]
    [InlineData(new string[] { "docker" }, new string[] { }, 0.0)]
    public void Given_EmptySet_When_ComputingMatchedPrecision_Then_FollowsEmptySetConvention(
        string[] predicted,
        string[] expected,
        double expectedValue)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(expected, []);
        var result = new RecruiterEvaluationResult(predicted, [], [], "Summary.");
        var metric = ConfusionMetric.Precision(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_MissingList_When_ComputingMissingPrecision_Then_UsesMissingSkillsOnly()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker"]);
        var result = new RecruiterEvaluationResult(["java"], ["docker", "git"], ["Learn Docker."], "Summary.");
        var metric = ConfusionMetric.Precision(SkillList.Missing);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_PerfectMatchWithWhitespace_When_ComputingMatchedRecall_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult([" c# ", "sql "], [], [], "Summary.");
        var metric = ConfusionMetric.Recall(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_DisjointSets_When_ComputingMatchedRecall_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["java"], [], [], "Summary.");
        var metric = ConfusionMetric.Recall(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_PartialOverlap_When_ComputingMatchedRecall_Then_ReturnsOneThird()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql", "docker"], []);
        var result = new RecruiterEvaluationResult(["c#", "java"], [], [], "Summary.");
        var metric = ConfusionMetric.Recall(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0.3333, 0.0001);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { }, 1.0)]
    [InlineData(new string[] { "java" }, new string[] { }, 0.0)]
    [InlineData(new string[] { }, new string[] { "docker" }, 0.0)]
    public void Given_EmptySet_When_ComputingMatchedRecall_Then_FollowsEmptySetConvention(
        string[] predicted,
        string[] expected,
        double expectedValue)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(expected, []);
        var result = new RecruiterEvaluationResult(predicted, [], [], "Summary.");
        var metric = ConfusionMetric.Recall(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_MissingList_When_ComputingMissingRecall_Then_UsesMissingSkillsOnly()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker", "git"]);
        var result = new RecruiterEvaluationResult(["java"], ["docker"], ["Learn Docker."], "Summary.");
        var metric = ConfusionMetric.Recall(SkillList.Missing);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_PerfectMatch_When_ComputingMatchedF1_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult(["c#", "sql"], [], [], "Summary.");
        var metric = ConfusionMetric.F1(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_DisjointSets_When_ComputingMatchedF1_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["java"], [], [], "Summary.");
        var metric = ConfusionMetric.F1(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_PartialOverlap_When_ComputingMatchedF1_Then_ReturnsHarmonicMean()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql", "docker"], []);
        var result = new RecruiterEvaluationResult(["c#", "java"], [], [], "Summary.");
        var metric = ConfusionMetric.F1(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(0.4, 0.0001);
    }

    [Theory]
    [InlineData(new string[] { }, new string[] { }, 1.0)]
    [InlineData(new string[] { }, new string[] { "docker" }, 0.0)]
    [InlineData(new string[] { "java" }, new string[] { }, 0.0)]
    public void Given_EmptySet_When_ComputingMatchedF1_Then_FollowsEmptySetConvention(
        string[] predicted,
        string[] expected,
        double expectedValue)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(expected, []);
        var result = new RecruiterEvaluationResult(predicted, [], [], "Summary.");
        var metric = ConfusionMetric.F1(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_PredictionWrittenAsAlias_When_ComputingMatchedF1WithAliases_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["  CSharp "], [], [], "Summary.");
        var metric = ConfusionMetric.F1(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, AliasNormalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_PredictionWrittenAsAlias_When_ComputingMatchedF1WithoutAliases_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["  CSharp "], [], [], "Summary.");
        var metric = ConfusionMetric.F1(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, NoAliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Theory]
    [InlineData("Precision", "Matched", "Matched precision")]
    [InlineData("Recall", "Matched", "Matched recall")]
    [InlineData("F1", "Matched", "Matched F1")]
    [InlineData("Precision", "Missing", "Missing precision")]
    [InlineData("Recall", "Missing", "Missing recall")]
    [InlineData("F1", "Missing", "Missing F1")]
    public void Given_FactoryAndSkillList_When_ReadingName_Then_ReturnsReportName(
        string factory,
        string list,
        string expectedName)
    {
        // Arrange
        var metric = Factories[factory](Lists[list]);

        // Act
        var name = metric.Name;

        // Assert
        name.Should().Be(expectedName);
    }
}
