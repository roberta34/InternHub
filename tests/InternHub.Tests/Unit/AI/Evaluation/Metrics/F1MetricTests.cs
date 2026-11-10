using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class F1MetricTests
{
    private static readonly SkillNormalizer AliasNormalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    private static readonly SkillNormalizer NoAliasNormalizer =
        new(new Dictionary<string, string>());

    [Fact]
    public void Given_PerfectMatch_When_ComputingMatchedF1_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult(["c#", "sql"], [], [], "Summary.");
        var metric = new F1Metric(SkillList.Matched);

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
        var metric = new F1Metric(SkillList.Matched);

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
        var metric = new F1Metric(SkillList.Matched);

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
        var metric = new F1Metric(SkillList.Matched);

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
        var metric = new F1Metric(SkillList.Matched);

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
        var metric = new F1Metric(SkillList.Matched);

        // Act
        var value = metric.Compute(evaluationCase, result, NoAliasNormalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_MatchedAndMissingLists_When_ReadingNames_Then_NamesIncludeList()
    {
        // Arrange
        var metrics = new[] { new F1Metric(SkillList.Matched), new F1Metric(SkillList.Missing) };

        // Act
        var names = metrics.Select(metric => metric.Name);

        // Assert
        names.Should().Equal("Matched F1", "Missing F1");
    }
}
