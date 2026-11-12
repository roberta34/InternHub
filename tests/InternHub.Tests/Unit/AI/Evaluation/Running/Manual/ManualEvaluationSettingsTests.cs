using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Running;
using InternHub.Tests.AI.Evaluation.Running.Manual;

namespace InternHub.Tests.Unit.AI.Evaluation.Running.Manual;

/// <summary>Unit tests for ManualEvaluationSettings.</summary>
public sealed class ManualEvaluationSettingsTests
{
    private static Func<string, string?> Reader(Dictionary<string, string?> values) =>
        key => values.GetValueOrDefault(key);

    [Fact]
    public void Given_RunsNotSet_When_Reading_Then_UsesDefaultOfFive()
    {
        // Arrange
        var read = Reader(new Dictionary<string, string?>());

        // Act
        var settings = ManualEvaluationSettings.FromEnvironment(read);

        // Assert
        settings.RunsPerCase.Should().Be(5);
    }

    [Fact]
    public void Given_RunsSetToThree_When_Reading_Then_UsesThree()
    {
        // Arrange
        var read = Reader(new Dictionary<string, string?> { ["INTERNHUB_AI_EVAL_RUNS"] = "3" });

        // Act
        var settings = ManualEvaluationSettings.FromEnvironment(read);

        // Assert
        settings.RunsPerCase.Should().Be(3);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("abc")]
    public void Given_InvalidRuns_When_Reading_Then_ThrowsSetupException(string runs)
    {
        // Arrange
        var read = Reader(new Dictionary<string, string?> { ["INTERNHUB_AI_EVAL_RUNS"] = runs });

        // Act
        var readSettings = () => ManualEvaluationSettings.FromEnvironment(read);

        // Assert
        readSettings.Should().Throw<EvaluationSetupException>()
            .WithMessage("*INTERNHUB_AI_EVAL_RUNS*");
    }

    [Fact]
    public void Given_NoVariablesSet_When_Reading_Then_ContextValuesAreNotSetAndOllamaValuesAreNull()
    {
        // Arrange
        var read = Reader(new Dictionary<string, string?>());

        // Act
        var settings = ManualEvaluationSettings.FromEnvironment(read);

        // Assert
        settings.Should().Be(new ManualEvaluationSettings(
            null,
            null,
            5,
            "not set",
            "not set",
            "not set",
            "not set"));
    }

    [Fact]
    public void Given_AllVariablesSet_When_Reading_Then_PassesValuesThrough()
    {
        // Arrange
        var read = Reader(new Dictionary<string, string?>
        {
            ["INTERNHUB_AI_EVAL_OLLAMA_ENDPOINT"] = "http://localhost:11434",
            ["INTERNHUB_AI_EVAL_OLLAMA_MODEL"] = "llama3.2:3b",
            ["INTERNHUB_AI_EVAL_RUNS"] = "2",
            ["INTERNHUB_AI_EVAL_TEMPERATURE"] = "0.1",
            ["INTERNHUB_AI_EVAL_PROMPT_VERSION"] = " internhub-recruiter-evaluation-v1 ",
            ["INTERNHUB_AI_EVAL_COMMIT"] = "abc1234",
            ["INTERNHUB_AI_EVAL_DATE"] = "2026-11-08"
        });

        // Act
        var settings = ManualEvaluationSettings.FromEnvironment(read);

        // Assert
        settings.Should().Be(new ManualEvaluationSettings(
            "http://localhost:11434",
            "llama3.2:3b",
            2,
            "0.1",
            "internhub-recruiter-evaluation-v1",
            "abc1234",
            "2026-11-08"));
    }
}
