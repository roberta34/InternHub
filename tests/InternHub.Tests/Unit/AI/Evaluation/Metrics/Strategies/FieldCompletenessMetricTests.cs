using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Strategies;

/// <summary>Unit tests for FieldCompletenessMetric.</summary>
public sealed class FieldCompletenessMetricTests
{
    private static readonly SkillNormalizer Normalizer = new(new Dictionary<string, string>());

    [Fact]
    public void Given_NoMissingSkillsAndNoSuggestions_When_ComputingCompleteness_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["c#"], [], [], "All skills matched.");
        var metric = new FieldCompletenessMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_MissingSkillsWithSuggestions_When_ComputingCompleteness_Then_ReturnsOne()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker"]);
        var result = new RecruiterEvaluationResult(["c#"], ["docker"], ["Learn Docker."], "One skill missing.");
        var metric = new FieldCompletenessMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_MissingSkillsWithoutSuggestions_When_ComputingCompleteness_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], ["docker"]);
        var result = new RecruiterEvaluationResult(["c#"], ["docker"], [], "One skill missing.");
        var metric = new FieldCompletenessMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankSummary_When_ComputingCompleteness_Then_ReturnsZero(string summary)
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["c#"], [], [], summary);
        var metric = new FieldCompletenessMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_NullMatchedSkills_When_ComputingCompleteness_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(null!, [], [], "Summary.");
        var metric = new FieldCompletenessMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_NullSuggestionsWithNoMissingSkills_When_ComputingCompleteness_Then_ReturnsZero()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#"], []);
        var result = new RecruiterEvaluationResult(["c#"], [], null!, "Summary.");
        var metric = new FieldCompletenessMetric();

        // Act
        var value = metric.Compute(evaluationCase, result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }
}
