using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class ConfusionCountsTests
{
    private static readonly SkillNormalizer Normalizer =
        new(new Dictionary<string, string> { ["csharp"] = "c#" });

    [Fact]
    public void Given_PartialOverlap_When_Counting_Then_ReturnsTruePositivesFalsePositivesAndFalseNegatives()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql", "docker"], []);
        var result = new RecruiterEvaluationResult(["c#", "java"], [], [], "Summary.");

        // Act
        var counts = ConfusionCounts.For(evaluationCase, result, Normalizer, SkillList.Matched);

        // Assert
        counts.Should().Be(new ConfusionCounts(1, 1, 2));
    }

    [Fact]
    public void Given_PredictionsDifferingByCaseWhitespaceAliasAndDuplicates_When_Counting_Then_CountsEachSkillOnce()
    {
        // Arrange
        var evaluationCase = EvaluationCaseBuilder.With(["c#", "sql"], []);
        var result = new RecruiterEvaluationResult([" C# ", "csharp", "SQL", "sql"], [], [], "Summary.");

        // Act
        var counts = ConfusionCounts.For(evaluationCase, result, Normalizer, SkillList.Matched);

        // Assert
        counts.Should().Be(new ConfusionCounts(2, 0, 0));
    }

    [Fact]
    public void Given_TwoCounts_When_Adding_Then_SumsEachComponent()
    {
        // Arrange
        var left = new ConfusionCounts(1, 2, 3);
        var right = new ConfusionCounts(4, 5, 6);

        // Act
        var sum = left + right;

        // Assert
        sum.Should().Be(new ConfusionCounts(5, 7, 9));
    }

    [Theory]
    [InlineData(0, 0, 0, 1.0, 1.0, 1.0)]
    [InlineData(0, 0, 2, 0.0, 0.0, 0.0)]
    [InlineData(0, 3, 0, 0.0, 0.0, 0.0)]
    [InlineData(2, 3, 2, 0.4, 0.5, 0.4444)]
    public void Given_Counts_When_ComputingScores_Then_FollowsDefinitionsAndEmptySetConventions(
        int truePositives,
        int falsePositives,
        int falseNegatives,
        double expectedPrecision,
        double expectedRecall,
        double expectedF1)
    {
        // Arrange
        var counts = new ConfusionCounts(truePositives, falsePositives, falseNegatives);

        // Act
        var scores = (counts.Precision, counts.Recall, counts.F1);

        // Assert
        scores.Precision.Should().BeApproximately(expectedPrecision, 0.0001);
        scores.Recall.Should().BeApproximately(expectedRecall, 0.0001);
        scores.F1.Should().BeApproximately(expectedF1, 0.0001);
    }
}
