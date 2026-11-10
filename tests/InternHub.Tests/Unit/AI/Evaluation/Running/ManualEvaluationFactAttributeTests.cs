using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Running;

namespace InternHub.Tests.Unit.AI.Evaluation.Running;

public sealed class ManualEvaluationFactAttributeTests
{
    [Theory]
    [InlineData(null, "INTERNHUB_AI_EVAL=1")]
    [InlineData("", "INTERNHUB_AI_EVAL=1")]
    [InlineData("0", "INTERNHUB_AI_EVAL=1")]
    [InlineData("true", "INTERNHUB_AI_EVAL=1")]
    public void Given_VariableNotEqualToOne_When_ResolvingSkipReason_Then_ReturnsReasonNamingTheVariable(
        string? value,
        string expectedFragment)
    {
        // Arrange
        var variableValue = value;

        // Act
        var reason = ManualEvaluationFactAttribute.SkipReasonFor(variableValue);

        // Assert
        reason.Should().Contain(expectedFragment);
    }

    [Fact]
    public void Given_VariableEqualToOne_When_ResolvingSkipReason_Then_ReturnsNull()
    {
        // Arrange
        var variableValue = "1";

        // Act
        var reason = ManualEvaluationFactAttribute.SkipReasonFor(variableValue);

        // Assert
        reason.Should().BeNull();
    }

    [Fact]
    public void Given_Attribute_When_InspectingType_Then_DerivesFromFactAttribute()
    {
        // Arrange
        var attributeType = typeof(ManualEvaluationFactAttribute);

        // Act
        var baseType = attributeType.BaseType;

        // Assert
        baseType.Should().Be(typeof(FactAttribute));
    }
}
