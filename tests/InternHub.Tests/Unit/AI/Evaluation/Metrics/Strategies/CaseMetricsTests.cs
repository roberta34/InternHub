using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Metrics.Strategies;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Strategies;

/// <summary>Unit tests for CaseMetrics.</summary>
public sealed class CaseMetricsTests
{
    [Theory]
    [InlineData("Matched precision", MetricKind.Semantic)]
    [InlineData("Matched recall", MetricKind.Semantic)]
    [InlineData("Matched F1", MetricKind.Semantic)]
    [InlineData("Missing precision", MetricKind.Semantic)]
    [InlineData("Missing recall", MetricKind.Semantic)]
    [InlineData("Missing F1", MetricKind.Semantic)]
    [InlineData("Hallucination rate", MetricKind.Semantic)]
    [InlineData("Contradiction", MetricKind.Semantic)]
    [InlineData("Coverage", MetricKind.Semantic)]
    [InlineData("Field completeness", MetricKind.Structural)]
    [InlineData("Suggestion coverage", MetricKind.Structural)]
    [InlineData("Suggestion mismatch", MetricKind.Structural)]
    public void Given_RegisteredMetric_When_ReadingKind_Then_MatchesSkillTable(string name, MetricKind expected)
    {
        // Arrange
        var metrics = CaseMetrics.Default;

        // Act
        var metric = metrics.Single(candidate => candidate.Name == name);

        // Assert
        metric.Kind.Should().Be(expected);
    }

    [Fact]
    public void Given_Registry_When_Counting_Then_ContainsTwelveUniquelyNamedMetrics()
    {
        // Arrange
        var metrics = CaseMetrics.Default;

        // Act
        var names = metrics.Select(metric => metric.Name).ToList();

        // Assert
        names.Should().HaveCount(12).And.OnlyHaveUniqueItems();
    }
}
