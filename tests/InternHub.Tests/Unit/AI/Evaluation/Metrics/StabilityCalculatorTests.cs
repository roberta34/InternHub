using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class StabilityCalculatorTests
{
    private static readonly SkillNormalizer Normalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    private static RecruiterEvaluationResult Run(string[] matched, string[] missing) =>
        new(matched, missing, ["Suggestion."], "Summary.");

    [Fact]
    public void Given_ThreeIdenticalRuns_When_ComputingMatchedStability_Then_ReturnsOne()
    {
        // Arrange
        var calculator = new StabilityCalculator(Normalizer);
        RecruiterEvaluationResult[] runs = [Run(["a", "b"], []), Run(["a", "b"], []), Run(["a", "b"], [])];

        // Act
        var value = calculator.Compute(runs, SkillList.Matched);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_RunsAbAbA_When_ComputingMatchedStability_Then_ReturnsMeanPairwiseJaccard()
    {
        // Arrange
        // Pairs: {a,b}-{a,b} = 1, {a,b}-{a} = 0.5, {a,b}-{a} = 0.5 -> (1 + 0.5 + 0.5) / 3
        var calculator = new StabilityCalculator(Normalizer);
        RecruiterEvaluationResult[] runs = [Run(["a", "b"], []), Run(["a", "b"], []), Run(["a"], [])];

        // Act
        var value = calculator.Compute(runs, SkillList.Matched);

        // Assert
        value.Should().BeApproximately(0.6667, 0.0001);
    }

    [Fact]
    public void Given_TwoEmptySets_When_ComputingMatchedStability_Then_ReturnsOne()
    {
        // Arrange
        var calculator = new StabilityCalculator(Normalizer);
        RecruiterEvaluationResult[] runs = [Run([], ["c"]), Run([], ["c"])];

        // Act
        var value = calculator.Compute(runs, SkillList.Matched);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_RunsDifferingByCaseWhitespaceAndAlias_When_ComputingMatchedStability_Then_ReturnsOne()
    {
        // Arrange
        var calculator = new StabilityCalculator(Normalizer);
        RecruiterEvaluationResult[] runs = [Run(["C#"], []), Run([" csharp "], [])];

        // Act
        var value = calculator.Compute(runs, SkillList.Matched);

        // Assert
        value.Should().BeApproximately(1, 0.0001);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void Given_FewerThanTwoResults_When_ComputingStability_Then_ThrowsArgumentException(int count)
    {
        // Arrange
        var calculator = new StabilityCalculator(Normalizer);
        var runs = Enumerable.Repeat(Run(["a"], []), count).ToList();

        // Act
        var compute = () => calculator.Compute(runs, SkillList.Matched);

        // Assert
        compute.Should().Throw<ArgumentException>()
            .WithParameterName("results");
    }

    [Fact]
    public void Given_RunsDifferingOnlyInMatched_When_ComputingForCase_Then_ScoresMatchedAndMissingSeparately()
    {
        // Arrange
        // Matched: {a,b}-{a} = 0.5; Missing: {c}-{c} = 1
        var calculator = new StabilityCalculator(Normalizer);
        RecruiterEvaluationResult[] runs = [Run(["a", "b"], ["c"]), Run(["a"], ["c"])];

        // Act
        var scores = calculator.ComputeForCase(runs);

        // Assert
        scores.Matched.Should().BeApproximately(0.5, 0.0001);
        scores.Missing.Should().BeApproximately(1, 0.0001);
    }

    [Fact]
    public void Given_TwoCases_When_ComputingMeanOverCases_Then_AveragesPerCaseScores()
    {
        // Arrange
        // Case 1: matched {a}-{a} = 1, missing {b}-{b} = 1
        // Case 2: matched {a,b}-{a} = 0.5, missing {c}-{d} = 0
        var calculator = new StabilityCalculator(Normalizer);
        IReadOnlyList<RecruiterEvaluationResult>[] resultsPerCase =
        [
            [Run(["a"], ["b"]), Run(["a"], ["b"])],
            [Run(["a", "b"], ["c"]), Run(["a"], ["d"])]
        ];

        // Act
        var scores = calculator.MeanOverCases(resultsPerCase);

        // Assert
        scores.Matched.Should().BeApproximately(0.75, 0.0001);
        scores.Missing.Should().BeApproximately(0.5, 0.0001);
    }

    [Fact]
    public void Given_NoCases_When_ComputingMeanOverCases_Then_ThrowsArgumentException()
    {
        // Arrange
        var calculator = new StabilityCalculator(Normalizer);
        IReadOnlyList<RecruiterEvaluationResult>[] resultsPerCase = [];

        // Act
        var compute = () => calculator.MeanOverCases(resultsPerCase);

        // Assert
        compute.Should().Throw<ArgumentException>()
            .WithParameterName("resultsPerCase");
    }
}
