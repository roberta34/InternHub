using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Core;

/// <summary>Unit tests for Ratio.</summary>
public sealed class RatioTests
{
    [Fact]
    public void Given_NonZeroDenominator_When_Dividing_Then_ReturnsQuotient()
    {
        // Arrange
        var numerator = 1;
        var denominator = 4;

        // Act
        var value = Ratio.Divide(numerator, denominator, 1);

        // Assert
        value.Should().BeApproximately(0.25, 0.0001);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(1.0)]
    public void Given_ZeroDenominator_When_Dividing_Then_ReturnsGivenEmptySetValue(double whenDenominatorIsZero)
    {
        // Arrange
        var numerator = 0;
        var denominator = 0;

        // Act
        var value = Ratio.Divide(numerator, denominator, whenDenominatorIsZero);

        // Assert
        value.Should().BeApproximately(whenDenominatorIsZero, 0.0001);
    }
}
