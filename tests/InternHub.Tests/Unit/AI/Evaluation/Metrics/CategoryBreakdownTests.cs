using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics;

public sealed class CategoryBreakdownTests
{
    private static readonly SkillNormalizer Normalizer = new(new Dictionary<string, string>());

    private static CategoryBreakdown CreateBreakdown() =>
        new(new MetricAggregator(CaseMetrics.Default, [SkillList.Matched, SkillList.Missing], Normalizer));

    // Clear: two perfect predictions; Negation: one disjoint prediction.
    private static List<(EvaluationCase Case, RecruiterEvaluationResult Result)> MixedCategories() =>
    [
        (EvaluationCaseBuilder.With(["c#"], []) with { Id = "clear-01" },
            new RecruiterEvaluationResult(["c#"], [], [], "Summary.")),
        (EvaluationCaseBuilder.With(["sql"], []) with { Id = "clear-02" },
            new RecruiterEvaluationResult(["sql"], [], [], "Summary.")),
        (EvaluationCaseBuilder.With(["docker"], []) with { Id = "negation-01", Category = EvaluationCategory.Negation },
            new RecruiterEvaluationResult(["java"], [], [], "Summary."))
    ];

    [Fact]
    public void Given_TwoCategories_When_Aggregating_Then_ReturnsOneEntryPerCategoryPresent()
    {
        // Arrange
        var breakdown = CreateBreakdown();
        var pairs = MixedCategories();

        // Act
        var scores = breakdown.Aggregate(pairs);

        // Assert
        scores.Keys.Should().BeEquivalentTo([EvaluationCategory.Clear, EvaluationCategory.Negation]);
    }

    [Fact]
    public void Given_TwoCategories_When_Aggregating_Then_ClearUsesOnlyItsOwnCases()
    {
        // Arrange
        var breakdown = CreateBreakdown();
        var pairs = MixedCategories();

        // Act
        var clear = breakdown.Aggregate(pairs)[EvaluationCategory.Clear];

        // Assert
        clear.CaseCount.Should().Be(2);
        clear.Macro["Matched F1"].Should().BeApproximately(1, 0.0001);
        clear.Micro["Matched"].Counts.Should().Be(new ConfusionCounts(2, 0, 0));
    }

    [Fact]
    public void Given_TwoCategories_When_Aggregating_Then_NegationUsesOnlyItsOwnCases()
    {
        // Arrange
        var breakdown = CreateBreakdown();
        var pairs = MixedCategories();

        // Act
        var negation = breakdown.Aggregate(pairs)[EvaluationCategory.Negation];

        // Assert
        negation.CaseCount.Should().Be(1);
        negation.Macro["Matched F1"].Should().BeApproximately(0, 0.0001);
        negation.Micro["Matched"].Counts.Should().Be(new ConfusionCounts(0, 1, 1));
    }

    [Fact]
    public void Given_NoPairs_When_Aggregating_Then_ReturnsEmptyBreakdown()
    {
        // Arrange
        var breakdown = CreateBreakdown();
        List<(EvaluationCase Case, RecruiterEvaluationResult Result)> pairs = [];

        // Act
        var scores = breakdown.Aggregate(pairs);

        // Assert
        scores.Should().BeEmpty();
    }
}
