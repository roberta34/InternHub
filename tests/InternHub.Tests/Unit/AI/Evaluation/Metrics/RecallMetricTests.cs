using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class RecallMetricTests
{
    private static readonly SkillNormalizer Normalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    [Fact]
    public void Given_PerfectMatchWithWhitespace_When_ComputingMatchedRecall_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult([" c# ", "sql "], [], [], "Summary.");
        var metric = new RecallMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_DisjointSets_When_ComputingMatchedRecall_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["java"], [], [], "Summary.");
        var metric = new RecallMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_PartialOverlap_When_ComputingMatchedRecall_Then_ReturnsOneThird()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql", "docker"], []);
        var result = new RecruiterEvaluationResult(["c#", "java"], [], [], "Summary.");
        var metric = new RecallMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

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
        var metric = new RecallMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_MissingList_When_ComputingMissingRecall_Then_UsesMissingSkillsOnly()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker", "git"]);
        var result = new RecruiterEvaluationResult(["java"], ["docker"], ["Learn Docker."], "Summary.");
        var metric = new RecallMetric(SkillList.Missing);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_MatchedAndMissingLists_When_ReadingNames_Then_NamesIncludeList()
    {
        // Arrange
        var metrics = new[] { new RecallMetric(SkillList.Matched), new RecallMetric(SkillList.Missing) };

        // Act
        var names = metrics.Select(metric => metric.Name);

        // Assert
        names.Should().Equal("Matched recall", "Missing recall");
    }
}
