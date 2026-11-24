using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.Unit.AI.Evaluation.Metrics.Core;

/// <summary>Unit tests for SkillNormalizer.</summary>
public sealed class SkillNormalizerTests
{
    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>
        {
            ["csharp"] = "c#",
            ["js"] = "javascript"
        };

    [Theory]
    [InlineData("C", "C#")]
    [InlineData("Java", "JavaScript")]
    [InlineData("SQL", "SQL Server")]
    public void Given_ConfusablePairAndDatasetAliases_When_Normalizing_Then_ReturnsDifferentSkills(
        string first,
        string second)
    {
        // Arrange
        var normalizer = new SkillNormalizer(EvaluationDatasetLoader.Load().Aliases);

        // Act
        var firstNormalized = normalizer.Normalize(first);
        var secondNormalized = normalizer.Normalize(second);

        // Assert
        firstNormalized.Should().NotBe(secondNormalized);
    }

    [Theory]
    [InlineData("  Docker ", "docker")]
    [InlineData("SQL", "sql")]
    [InlineData("\tGit\n", "git")]
    public void Given_SkillWithCaseAndWhitespace_When_Normalizing_Then_ReturnsTrimmedLowercase(
        string skill,
        string expected)
    {
        // Arrange
        var normalizer = new SkillNormalizer(Aliases);

        // Act
        var normalized = normalizer.Normalize(skill);

        // Assert
        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("csharp", "c#")]
    [InlineData("js", "javascript")]
    public void Given_Alias_When_Normalizing_Then_ReturnsCanonicalSkill(string skill, string expected)
    {
        // Arrange
        var normalizer = new SkillNormalizer(Aliases);

        // Act
        var normalized = normalizer.Normalize(skill);

        // Assert
        normalized.Should().Be(expected);
    }

    [Fact]
    public void Given_AliasWithCaseAndWhitespace_When_Normalizing_Then_ReturnsCanonicalSkill()
    {
        // Arrange
        var normalizer = new SkillNormalizer(Aliases);

        // Act
        var normalized = normalizer.Normalize("  CSharp ");

        // Assert
        normalized.Should().Be("c#");
    }

    [Fact]
    public void Given_SkillWithoutAlias_When_Normalizing_Then_ReturnsSkillUnchanged()
    {
        // Arrange
        var normalizer = new SkillNormalizer(Aliases);

        // Act
        var normalized = normalizer.Normalize("kubernetes");

        // Assert
        normalized.Should().Be("kubernetes");
    }

    [Fact]
    public void Given_DuplicatesDifferingByCaseAndAlias_When_ConvertingToSet_Then_RemovesDuplicates()
    {
        // Arrange
        var normalizer = new SkillNormalizer(Aliases);
        string[] skills = ["C#", "csharp", " c# ", "Docker", "docker"];

        // Act
        var set = normalizer.ToSet(skills);

        // Assert
        set.Should().BeEquivalentTo(["c#", "docker"]);
    }

    [Fact]
    public void Given_EmptyList_When_ConvertingToSet_Then_ReturnsEmptySet()
    {
        // Arrange
        var normalizer = new SkillNormalizer(Aliases);
        string[] skills = [];

        // Act
        var set = normalizer.ToSet(skills);

        // Assert
        set.Should().BeEmpty();
    }

    [Fact]
    public void Given_NullAliasMap_When_Constructing_Then_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyDictionary<string, string> aliases = null!;

        // Act
        var construct = () => new SkillNormalizer(aliases);

        // Assert
        construct.Should().Throw<ArgumentNullException>()
            .WithParameterName("aliases");
    }
}
