using System.Text.RegularExpressions;
using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Dataset;
using InternHub.Tests.AI.Evaluation.Metrics.Core;

namespace InternHub.Tests.Unit.AI.Evaluation.Dataset;

/// <summary>Unit tests for EvaluationDataset.</summary>
public sealed class EvaluationDatasetTests
{
    private static readonly EvaluationDataset Dataset = EvaluationDatasetLoader.Load();

    private static readonly SkillNormalizer Normalizer = new(Dataset.Aliases);

    public static TheoryData<string> CaseIds() => new(Dataset.Cases.Select(evaluationCase => evaluationCase.Id));

    [Fact]
    public void Given_DatasetFile_When_Loading_Then_IsMarkedSynthetic()
    {
        // Arrange
        var path = EvaluationDatasetLoader.DefaultPath;

        // Act
        var dataset = EvaluationDatasetLoader.Load(path);

        // Assert
        dataset.Synthetic.Should().BeTrue();
        dataset.DatasetVersion.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_Dataset_When_ReadingCaseIds_Then_IdsAreUnique()
    {
        // Arrange
        var cases = Dataset.Cases;

        // Act
        var ids = cases.Select(evaluationCase => evaluationCase.Id).ToList();

        // Assert
        ids.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Given_Dataset_When_ReadingVersion_Then_IsTwoPointZero()
    {
        // Arrange
        var dataset = Dataset;

        // Act
        var version = dataset.DatasetVersion;

        // Assert
        version.Should().Be("2.0.0");
    }

    [Fact]
    public void Given_Dataset_When_CountingCases_Then_ContainsExactlyTwentySix()
    {
        // Arrange
        var cases = Dataset.Cases;

        // Act
        var count = cases.Count;

        // Assert
        count.Should().Be(26);
    }

    [Theory]
    [InlineData(EvaluationCategory.Clear, 3)]
    [InlineData(EvaluationCategory.Synonyms, 2)]
    [InlineData(EvaluationCategory.Negation, 4)]
    [InlineData(EvaluationCategory.Implicit, 2)]
    [InlineData(EvaluationCategory.Noise, 3)]
    [InlineData(EvaluationCategory.Minimal, 2)]
    [InlineData(EvaluationCategory.PromptInjection, 4)]
    [InlineData(EvaluationCategory.AmbiguousTerms, 2)]
    [InlineData(EvaluationCategory.SuggestionMismatch, 2)]
    [InlineData(EvaluationCategory.UnsupportedEvidence, 2)]
    public void Given_Dataset_When_CountingCasesPerCategory_Then_MatchesRequiredDistribution(
        EvaluationCategory category,
        int expectedCount)
    {
        // Arrange
        var cases = Dataset.Cases;

        // Act
        var count = cases.Count(evaluationCase => evaluationCase.Category == category);

        // Assert
        count.Should().Be(expectedCount);
    }

    [Fact]
    public void Given_Aliases_When_ComparingTargetsWithKeys_Then_NoTargetIsAlsoAnAlias()
    {
        // Arrange
        var aliases = Dataset.Aliases;

        // Act
        var chainedTargets = aliases.Values.Intersect(aliases.Keys).ToList();

        // Assert
        chainedTargets.Should().BeEmpty();
    }

    [Fact]
    public void Given_Aliases_When_CheckingKeysAndTargets_Then_AllAreTrimmedAndLowercase()
    {
        // Arrange
        var aliases = Dataset.Aliases;

        // Act
        var unnormalizedValues = aliases.Keys
            .Concat(aliases.Values)
            .Where(value => value != value.Trim().ToLowerInvariant())
            .ToList();

        // Assert
        unnormalizedValues.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Given_Case_When_ReadingId_Then_IsPrefixedByKebabCaseCategory(string caseId)
    {
        // Arrange
        var evaluationCase = FindCase(caseId);
        var prefix = Regex.Replace(evaluationCase.Category.ToString(), "(?<!^)([A-Z])", "-$1").ToLowerInvariant();

        // Act
        var matchesPattern = Regex.IsMatch(caseId, $"^{Regex.Escape(prefix)}-[0-9]{{2}}$");

        // Assert
        matchesPattern.Should().BeTrue($"'{caseId}' must be '{prefix}-NN'");
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Given_Case_When_NormalizingLabels_Then_MatchedAndMissingAreDisjoint(string caseId)
    {
        // Arrange
        var evaluationCase = FindCase(caseId);
        var matched = Normalizer.ToSet(evaluationCase.ExpectedMatched);
        var missing = Normalizer.ToSet(evaluationCase.ExpectedMissing);

        // Act
        var overlap = matched.Intersect(missing).ToList();

        // Assert
        overlap.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Given_Case_When_NormalizingLabels_Then_UnionEqualsRequiredSkills(string caseId)
    {
        // Arrange
        var evaluationCase = FindCase(caseId);
        var required = Normalizer.ToSet(evaluationCase.RequiredSkills);

        // Act
        var union = Normalizer.ToSet(evaluationCase.ExpectedMatched.Concat(evaluationCase.ExpectedMissing));

        // Assert
        union.Should().BeEquivalentTo(required);
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Given_Case_When_SearchingRequirementsText_Then_EveryRequiredSkillOrAliasOccurs(string caseId)
    {
        // Arrange
        var evaluationCase = FindCase(caseId);
        var requirements = evaluationCase.RequirementsText.ToLowerInvariant();

        // Act
        var absentSkills = evaluationCase.RequiredSkills
            .Where(skill => !SpellingsOf(skill).Any(spelling => requirements.Contains(spelling, StringComparison.Ordinal)))
            .ToList();

        // Assert
        absentSkills.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(CaseIds))]
    public void Given_Case_When_ReadingRationale_Then_IsNotBlank(string caseId)
    {
        // Arrange
        var evaluationCase = FindCase(caseId);

        // Act
        var rationale = evaluationCase.Rationale;

        // Assert
        rationale.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Given_Dataset_When_ReadingCategories_Then_EveryCategoryHasCases()
    {
        // Arrange
        var categories = Enum.GetValues<EvaluationCategory>();

        // Act
        var emptyCategories = categories
            .Where(category => Dataset.Cases.All(evaluationCase => evaluationCase.Category != category))
            .ToList();

        // Assert
        categories.Should().HaveCount(10);
        emptyCategories.Should().BeEmpty();
    }

    [Theory]
    [MemberData(nameof(UnsupportedEvidenceCaseIds))]
    public void Given_UnsupportedEvidenceCase_When_ReadingLabels_Then_HasRequiredSkillsAndNamesTechnologyOutsideThem(
        string caseId)
    {
        // Arrange
        var evaluationCase = FindCase(caseId);
        var required = Normalizer.ToSet(evaluationCase.RequiredSkills);

        // Act
        var named = Regex.Match(evaluationCase.Rationale, "^Unsupported: ([^.]+)\\.");

        // Assert
        required.Should().NotBeEmpty();
        named.Success.Should().BeTrue("the rationale must start with 'Unsupported: <technology>.'");
        required.Should().NotContain(Normalizer.Normalize(named.Groups[1].Value));
        evaluationCase.CvText.Should().ContainEquivalentOf(named.Groups[1].Value);
    }

    [Theory]
    [InlineData("c", "c#")]
    [InlineData("c#", "c")]
    [InlineData("java", "javascript")]
    [InlineData("javascript", "java")]
    [InlineData("sql", "sql server")]
    [InlineData("sql server", "sql")]
    public void Given_ConfusableSkill_When_NormalizingWithDatasetAliases_Then_NeverYieldsTheOtherSkill(
        string skill,
        string confusable)
    {
        // Arrange
        var normalizer = Normalizer;

        // Act
        var normalized = normalizer.Normalize(skill);

        // Assert
        normalized.Should().NotBe(confusable);
    }

    public static TheoryData<string> UnsupportedEvidenceCaseIds() =>
        new(Dataset.Cases
            .Where(evaluationCase => evaluationCase.Category == EvaluationCategory.UnsupportedEvidence)
            .Select(evaluationCase => evaluationCase.Id));

    private static EvaluationCase FindCase(string caseId) =>
        Dataset.Cases.Single(evaluationCase => evaluationCase.Id == caseId);

    private static IEnumerable<string> SpellingsOf(string skill)
    {
        var canonical = Normalizer.Normalize(skill);

        return Dataset.Aliases
            .Where(alias => Normalizer.Normalize(alias.Value) == canonical)
            .Select(alias => alias.Key.ToLowerInvariant())
            .Append(canonical);
    }
}
