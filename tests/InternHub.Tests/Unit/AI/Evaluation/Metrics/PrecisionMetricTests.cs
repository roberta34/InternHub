using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class PrecisionMetricTests
{
    private static readonly SkillNormalizer Normalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    [Fact]
    public void Given_PerfectMatchWithDifferentCase_When_ComputingMatchedPrecision_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult(["C#", "SQL"], [], [], "Summary.");
        var metric = new PrecisionMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_DisjointSets_When_ComputingMatchedPrecision_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["java"], [], [], "Summary.");
        var metric = new PrecisionMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_PartialOverlap_When_ComputingMatchedPrecision_Then_ReturnsHalf()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql", "docker"], []);
        var result = new RecruiterEvaluationResult(["c#", "java"], [], [], "Summary.");
        var metric = new PrecisionMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

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
        var metric = new PrecisionMetric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(expectedValue, 0.0001);
    }

    [Fact]
    public void Given_MissingList_When_ComputingMissingPrecision_Then_UsesMissingSkillsOnly()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker"]);
        var result = new RecruiterEvaluationResult(["java"], ["docker", "git"], ["Learn Docker."], "Summary.");
        var metric = new PrecisionMetric(SkillList.Missing);

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_MatchedAndMissingLists_When_ReadingNames_Then_NamesIncludeList()
    {
        // Arrange
        var metrics = new[] { new PrecisionMetric(SkillList.Matched), new PrecisionMetric(SkillList.Missing) };

        // Act
        var names = metrics.Select(metric => metric.Name);

        // Assert
        names.Should().Equal("Matched precision", "Missing precision");
    }
}
