using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics.Core;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;
using InternHub.Tests.Unit.AI.Evaluation.TestDoubles;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Strategies;

/// <summary>Unit tests for SuggestionAssociationMetric.</summary>
public sealed class SuggestionAssociationMetricTests
{
    private static readonly SkillNormalizer Normalizer = new(
        new Dictionary<string, string>
        {
            ["k8s"] = "kubernetes",
            ["js"] = "javascript"
        });

    [Fact]
    public void Given_NoReportedMissingSkill_When_ComputingCoverage_Then_ReturnsOne()
    {
        // Arrange
        var result = new RecruiterEvaluationResult(["c#"], [], [], "Summary.");

        // Act
        var value = SuggestionAssociationMetric.Coverage().Compute(EvaluationCaseBuilder.With(["c#"], []), result, Normalizer);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_NoSuggestions_When_ComputingMismatch_Then_ReturnsZero()
    {
        // Arrange
        var result = new RecruiterEvaluationResult([], ["docker"], [], "Summary.");

        // Act
        var value = SuggestionAssociationMetric.Mismatch().Compute(EvaluationCaseBuilder.With([], ["docker"]), result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_MissingSkillsButNoSuggestions_When_ComputingCoverage_Then_ReturnsZero()
    {
        // Arrange
        // 0 of 2 missing skills mentioned -> 0 / 2 = 0.
        var result = new RecruiterEvaluationResult([], ["docker", "git"], [], "Summary.");

        // Act
        var value = SuggestionAssociationMetric.Coverage().Compute(EvaluationCaseBuilder.With([], ["docker", "git"]), result, Normalizer);

        // Assert
        value.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_EverySuggestionNamesAMissingSkill_When_ComputingBothScores_Then_CoverageOneAndMismatchZero()
    {
        // Arrange
        // Missing {docker, git}; both named -> coverage 2 / 2 = 1; mismatched suggestions 0 / 2 = 0.
        var result = new RecruiterEvaluationResult(
            [],
            ["Docker", "Git"],
            ["Learn Docker basics.", "Version a project with git."],
            "Summary.");
        var evaluationCase = EvaluationCaseBuilder.With([], ["docker", "git"]);

        // Act
        var coverage = SuggestionAssociationMetric.Coverage().Compute(evaluationCase, result, Normalizer);
        var mismatch = SuggestionAssociationMetric.Mismatch().Compute(evaluationCase, result, Normalizer);

        // Assert
        coverage.Should().BeApproximately(1, 0.0001);
        mismatch.Should().BeApproximately(0, 0.0001);
    }

    [Fact]
    public void Given_NoSuggestionNamesAMissingSkill_When_ComputingBothScores_Then_CoverageZeroAndMismatchOne()
    {
        // Arrange
        // Missing {docker}; suggestions name only other topics -> coverage 0 / 1 = 0; mismatch 2 / 2 = 1.
        var result = new RecruiterEvaluationResult(
            [],
            ["docker"],
            ["Improve the layout of your CV.", "Practice more C#."],
            "Summary.");
        var evaluationCase = EvaluationCaseBuilder.With([], ["docker"]);

        // Act
        var coverage = SuggestionAssociationMetric.Coverage().Compute(evaluationCase, result, Normalizer);
        var mismatch = SuggestionAssociationMetric.Mismatch().Compute(evaluationCase, result, Normalizer);

        // Assert
        coverage.Should().BeApproximately(0, 0.0001);
        mismatch.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_PartialAssociation_When_ComputingBothScores_Then_ReturnsHandComputedRatios()
    {
        // Arrange
        // Missing {docker, git, linux}; only docker is named -> coverage 1 / 3 = 0.3333.
        // Suggestions: 1 names docker, 1 names nothing -> mismatch 1 / 2 = 0.5.
        var result = new RecruiterEvaluationResult(
            [],
            ["docker", "git", "linux"],
            ["Containerise a project with Docker.", "Write a clearer summary."],
            "Summary.");
        var evaluationCase = EvaluationCaseBuilder.With([], ["docker", "git", "linux"]);

        // Act
        var coverage = SuggestionAssociationMetric.Coverage().Compute(evaluationCase, result, Normalizer);
        var mismatch = SuggestionAssociationMetric.Mismatch().Compute(evaluationCase, result, Normalizer);

        // Assert
        coverage.Should().BeApproximately(1.0 / 3, 0.0001);
        mismatch.Should().BeApproximately(0.5, 0.0001);
    }

    [Theory]
    [InlineData("kubernetes", "Deploy a demo to a k8s cluster.", 1)]
    [InlineData("k8s", "Learn Kubernetes fundamentals.", 1)]
    [InlineData("java", "Build a page in JavaScript.", 0)]
    [InlineData("c", "Write a small tool in C#.", 0)]
    [InlineData("sql", "Use SQL.", 1)]
    [InlineData("node.js", "Build a CLI with Node.js.", 1)]
    [InlineData("js", "Build a CLI with Node.js.", 0)]
    public void Given_SuggestionText_When_ComputingCoverage_Then_MatchesOnlyWholeTermsAndAliases(
        string missingSkill,
        string suggestion,
        double expected)
    {
        // Arrange
        var result = new RecruiterEvaluationResult([], [missingSkill], [suggestion], "Summary.");

        // Act
        var value = SuggestionAssociationMetric.Coverage().Compute(EvaluationCaseBuilder.With([], [missingSkill]), result, Normalizer);

        // Assert
        value.Should().BeApproximately(expected, 0.0001);
    }

    [Theory]
    [InlineData("Suggestion coverage")]
    [InlineData("Suggestion mismatch")]
    public void Given_FactoryMethod_When_ReadingNameAndKind_Then_IsNamedAndStructural(string expectedName)
    {
        // Arrange
        var metrics = new[] { SuggestionAssociationMetric.Coverage(), SuggestionAssociationMetric.Mismatch() };

        // Act
        var metric = metrics.Single(candidate => candidate.Name == expectedName);

        // Assert
        metric.Kind.Should().Be(MetricKind.Structural);
    }
}
