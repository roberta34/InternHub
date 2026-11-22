using FluentAssertions;
using InternHub.Api.Infrastructure.AI.Baseline;
using InternHub.Api.Infrastructure.AI.Fallback;

namespace InternHub.Tests.Unit.AI.Fallback;

public sealed class ProviderTraceDecoratorTests
{
    [Fact]
    public async Task Given_BaselineProvider_When_Analyzing_Then_TracesSelectedProviderWithoutFallback()
    {
        // Arrange
        var trace = new AiExecutionTrace();
        var decorator = new ProviderTraceDecorator(
            new KeywordRecruiterEvaluationService(),
            trace);

        // Act
        var result = await decorator.AnalyzeAsync("Python", "Python");

        // Assert
        result.MatchedSkills.Should().ContainSingle().Which.Should().Be("Python");
        trace.Provider.Should().Be("Baseline");
        trace.FallbackUsed.Should().BeFalse();
    }
}
