using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Api.Infrastructure.AI.Fallback;
using InternHub.Tests.Unit.AI.Fallback.Fakes;

namespace InternHub.Tests.Unit.AI.Fallback;

public sealed class OllamaFallbackDecoratorTests
{
    [Fact]
    public async Task Given_PrimarySucceeds_When_Analyzing_Then_DoesNotCallFallbackAndTracesOllama()
    {
        // Arrange
        var primaryResult = new RecruiterEvaluationResult(
            ["C#"],
            [],
            [],
            "Primary result.");
        var primary = new FakeRecruiterEvaluationService { Result = primaryResult };
        var fallback = new FakeRecruiterEvaluationService();
        var trace = new AiExecutionTrace();
        var decorator = new OllamaFallbackDecorator(primary, fallback, trace);

        // Act
        var result = await decorator.AnalyzeAsync("CV", "Requirements");

        // Assert
        result.Should().BeSameAs(primaryResult);
        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(0);
        trace.Provider.Should().Be("Ollama");
        trace.FallbackUsed.Should().BeFalse();
    }

    [Theory]
    [MemberData(nameof(EligibleExceptions))]
    public async Task Given_EligiblePrimaryException_When_Analyzing_Then_CallsBaselineOnceAndTracesFallback(
        Exception exception)
    {
        // Arrange
        var fallbackResult = new RecruiterEvaluationResult(
            [],
            ["Docker"],
            [],
            "Fallback result.");
        var primary = new FakeRecruiterEvaluationService
        {
            ExceptionToThrow = exception
        };
        var fallback = new FakeRecruiterEvaluationService
        {
            Result = fallbackResult
        };
        var trace = new AiExecutionTrace();
        var decorator = new OllamaFallbackDecorator(primary, fallback, trace);

        // Act
        var result = await decorator.AnalyzeAsync("CV", "Requirements");

        // Assert
        result.Should().BeSameAs(fallbackResult);
        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(1);
        trace.Provider.Should().Be("Baseline");
        trace.FallbackUsed.Should().BeTrue();
    }

    [Fact]
    public async Task Given_NoneligiblePrimaryException_When_Analyzing_Then_PropagatesWithoutCallingFallback()
    {
        // Arrange
        var exception = new ArgumentException("Invalid evaluation input.");
        var primary = new FakeRecruiterEvaluationService
        {
            ExceptionToThrow = exception
        };
        var fallback = new FakeRecruiterEvaluationService();
        var decorator = new OllamaFallbackDecorator(primary, fallback, new AiExecutionTrace());
        var action = () => decorator.AnalyzeAsync("CV", "Requirements");

        // Act
        var thrownException = await action.Should().ThrowAsync<ArgumentException>();

        // Assert
        thrownException.Which.Should().BeSameAs(exception);
        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_ClientCancellation_When_Analyzing_Then_PropagatesWithoutCallingFallback()
    {
        // Arrange
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();
        var cancellationToken = cancellationSource.Token;
        var exception = new OperationCanceledException(cancellationToken);
        var primary = new FakeRecruiterEvaluationService
        {
            ExceptionToThrow = exception
        };
        var fallback = new FakeRecruiterEvaluationService();
        var decorator = new OllamaFallbackDecorator(primary, fallback, new AiExecutionTrace());
        var action = () => decorator.AnalyzeAsync("CV", "Requirements", cancellationToken);

        // Act
        var thrownException = await action.Should().ThrowAsync<OperationCanceledException>();

        // Assert
        thrownException.Which.Should().BeSameAs(exception);
        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_BothProvidersFail_When_Analyzing_Then_PropagatesBaselineException()
    {
        // Arrange
        var baselineException = new InvalidOperationException("Baseline failed.");
        var primary = new FakeRecruiterEvaluationService
        {
            ExceptionToThrow = new HttpRequestException("Primary failed.")
        };
        var fallback = new FakeRecruiterEvaluationService
        {
            ExceptionToThrow = baselineException
        };
        var trace = new AiExecutionTrace();
        var decorator = new OllamaFallbackDecorator(primary, fallback, trace);
        var action = () => decorator.AnalyzeAsync("CV", "Requirements");

        // Act
        var thrownException = await action.Should().ThrowAsync<InvalidOperationException>();

        // Assert
        thrownException.Which.Should().BeSameAs(baselineException);
        primary.CallCount.Should().Be(1);
        fallback.CallCount.Should().Be(1);
        trace.Provider.Should().Be("Baseline");
        trace.FallbackUsed.Should().BeTrue();
    }

    public static TheoryData<Exception> EligibleExceptions =>
        new()
        {
            new HttpRequestException("Temporary transport failure."),
            new OperationCanceledException("Provider timeout."),
            new InvalidOperationException("Temporary provider failure.")
        };
}
