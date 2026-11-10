using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class ContradictionMetricTests
{
    private static readonly SkillNormalizer Normalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    [Fact]
    public void Given_DisjointMatchedAndMissing_When_ComputingContradiction_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker"]);
        var result = new RecruiterEvaluationResult(["c#"], ["docker"], ["Learn Docker."], "Summary.");
        var metric = new ContradictionMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Theory]
    [InlineData("C#", "c#")]
    [InlineData("csharp", "c#")]
    [InlineData(" c# ", "c#")]
    public void Given_SameSkillInBothListsAfterNormalization_When_ComputingContradiction_Then_ReturnsOne(
        string matchedSkill,
        string missingSkill)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult([matchedSkill], [missingSkill], ["Practice C#."], "Summary.");
        var metric = new ContradictionMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_BothListsEmpty_When_ComputingContradiction_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult([], [], [], "Summary.");
        var metric = new ContradictionMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_Metric_When_ReadingName_Then_ReturnsContradiction()
    {
        // Arrange
        var metric = new ContradictionMetric();

        // Act
        var name = metric.Name;

        // Assert
        name.Should().Be("Contradiction");
    }
}
