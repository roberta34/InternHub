using FluentAssertions;
using InternHub.Api.Features.RecruiterEvaluation;
using InternHub.Tests.AI.Evaluation.Running.Outcomes;

namespace InternHub.Tests.Unit.AI.Evaluation.Running.Outcomes;

/// <summary>Unit tests for OutcomeClassifier.</summary>
public sealed class OutcomeClassifierTests
{
    [Fact]
    public void Given_InvalidOperationException_When_Classifying_Then_ReturnsInvalidResponse()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var exception = new InvalidOperationException("Invalid model output.");

        // Act
        var classified = classifier.TryClassify(exception, CancellationToken.None, out var kind);

        // Assert
        classified.Should().BeTrue();
        kind.Should().Be(OutcomeKind.InvalidResponse);
    }

    [Fact]
    public void Given_HttpRequestException_When_Classifying_Then_ReturnsInfrastructureUnavailable()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var exception = new HttpRequestException("Connection refused.");

        // Act
        var classified = classifier.TryClassify(exception, CancellationToken.None, out var kind);

        // Assert
        classified.Should().BeTrue();
        kind.Should().Be(OutcomeKind.InfrastructureUnavailable);
    }

    [Fact]
    public void Given_TimeoutNotRequestedByCaller_When_Classifying_Then_ReturnsInfrastructureUnavailable()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var exception = new TaskCanceledException("The request timed out.");

        // Act
        var classified = classifier.TryClassify(exception, CancellationToken.None, out var kind);

        // Assert
        classified.Should().BeTrue();
        kind.Should().Be(OutcomeKind.InfrastructureUnavailable);
    }

    [Fact]
    public void Given_CancellationRequestedByCaller_When_Classifying_Then_IsNotClassified()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var exception = new TaskCanceledException("Cancelled by caller.");

        // Act
        var classified = classifier.TryClassify(exception, cancellation.Token, out _);

        // Assert
        classified.Should().BeFalse();
    }

    [Fact]
    public void Given_UnknownExceptionType_When_Classifying_Then_IsNotClassified()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var exception = new NotSupportedException("Unexpected.");

        // Act
        var classified = classifier.TryClassify(exception, CancellationToken.None, out _);

        // Assert
        classified.Should().BeFalse();
    }

    [Fact]
    public void Given_PlainOperationCanceledException_When_Classifying_Then_IsNotClassified()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var exception = new OperationCanceledException("Not a timeout type.");

        // Act
        var classified = classifier.TryClassify(exception, CancellationToken.None, out _);

        // Assert
        classified.Should().BeFalse();
    }

    [Fact]
    public void Given_CompleteResult_When_ClassifyingResult_Then_ReturnsSuccess()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var result = new RecruiterEvaluationResult(["c#"], ["docker"], ["Learn Docker."], "Summary.");

        // Act
        var kind = classifier.ClassifyResult(result);

        // Assert
        kind.Should().Be(OutcomeKind.Success);
    }

    [Fact]
    public void Given_NullMatchedSkills_When_ClassifyingResult_Then_ReturnsInvalidResponse()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var result = new RecruiterEvaluationResult(null!, [], [], "Summary.");

        // Act
        var kind = classifier.ClassifyResult(result);

        // Assert
        kind.Should().Be(OutcomeKind.InvalidResponse);
    }

    [Fact]
    public void Given_NullMissingSkills_When_ClassifyingResult_Then_ReturnsInvalidResponse()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var result = new RecruiterEvaluationResult([], null!, [], "Summary.");

        // Act
        var kind = classifier.ClassifyResult(result);

        // Assert
        kind.Should().Be(OutcomeKind.InvalidResponse);
    }

    [Fact]
    public void Given_NullSuggestions_When_ClassifyingResult_Then_ReturnsInvalidResponse()
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var result = new RecruiterEvaluationResult([], [], null!, "Summary.");

        // Act
        var kind = classifier.ClassifyResult(result);

        // Assert
        kind.Should().Be(OutcomeKind.InvalidResponse);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Given_BlankSummary_When_ClassifyingResult_Then_ReturnsInvalidResponse(string summary)
    {
        // Arrange
        var classifier = new OutcomeClassifier();
        var result = new RecruiterEvaluationResult(["c#"], [], [], summary);

        // Act
        var kind = classifier.ClassifyResult(result);

        // Assert
        kind.Should().Be(OutcomeKind.InvalidResponse);
    }
}
