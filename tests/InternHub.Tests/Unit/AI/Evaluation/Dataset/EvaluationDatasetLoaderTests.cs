using System.Text.Json;
using FluentAssertions;
using InternHub.Tests.AI.Evaluation.Dataset;

namespace InternHub.Tests.Unit.AI.Evaluation.Dataset;

public sealed class EvaluationDatasetLoaderTests
{
    private const string ValidJson = """
        {
          "datasetVersion": "test-1",
          "synthetic": true,
          "aliases": { "csharp": "c#" },
          "cases": [
            {
              "id": "negation-01",
              "category": "Negation",
              "cvText": "Fictional CV text.",
              "requirementsText": "We need C# and Docker.",
              "requiredSkills": ["c#", "docker"],
              "expectedMatched": ["c#"],
              "expectedMissing": ["docker"],
              "rationale": "Docker is named only to state it is missing."
            }
          ]
        }
        """;

    [Fact]
    public void Given_ValidJson_When_Parsing_Then_ReturnsDatasetWithAllFields()
    {
        // Arrange
        var json = ValidJson;

        // Act
        var dataset = EvaluationDatasetLoader.Parse(json);

        // Assert
        dataset.DatasetVersion.Should().Be("test-1");
        dataset.Synthetic.Should().BeTrue();
        dataset.Aliases.Should().Equal(new Dictionary<string, string> { ["csharp"] = "c#" });
        dataset.Cases.Should().ContainSingle()
            .Which.Should().BeEquivalentTo(new EvaluationCase(
                "negation-01",
                EvaluationCategory.Negation,
                "Fictional CV text.",
                "We need C# and Docker.",
                ["c#", "docker"],
                ["c#"],
                ["docker"],
                "Docker is named only to state it is missing."));
    }

    [Fact]
    public void Given_MalformedJson_When_Parsing_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = "{ \"datasetVersion\": ";

        // Act
        var parse = () => EvaluationDatasetLoader.Parse(json);

        // Assert
        parse.Should().Throw<InvalidOperationException>()
            .WithMessage("Evaluation dataset JSON is malformed*")
            .WithInnerException<JsonException>();
    }

    [Fact]
    public void Given_UnknownCategory_When_Parsing_Then_ThrowsInvalidOperationExceptionNamingCategoryPath()
    {
        // Arrange
        var json = ValidJson.Replace("\"Negation\"", "\"Sarcasm\"");

        // Act
        var parse = () => EvaluationDatasetLoader.Parse(json);

        // Assert
        parse.Should().Throw<InvalidOperationException>()
            .WithMessage("*$.cases[0].category*");
    }

    [Fact]
    public void Given_EmptyCaseList_When_Parsing_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = """{ "datasetVersion": "test-1", "synthetic": true, "aliases": {}, "cases": [] }""";

        // Act
        var parse = () => EvaluationDatasetLoader.Parse(json);

        // Assert
        parse.Should().Throw<InvalidOperationException>()
            .WithMessage("Evaluation dataset contains no cases.");
    }

    [Fact]
    public void Given_MissingCaseList_When_Parsing_Then_ThrowsInvalidOperationException()
    {
        // Arrange
        var json = """{ "datasetVersion": "test-1", "synthetic": true, "aliases": {} }""";

        // Act
        var parse = () => EvaluationDatasetLoader.Parse(json);

        // Assert
        parse.Should().Throw<InvalidOperationException>()
            .WithMessage("Evaluation dataset contains no cases.");
    }

    [Fact]
    public void Given_NonExistentPath_When_Loading_Then_ThrowsFileNotFoundExceptionNamingPath()
    {
        // Arrange
        var path = Path.Combine(AppContext.BaseDirectory, $"missing-dataset-{Guid.NewGuid():N}.json");

        // Act
        var load = () => EvaluationDatasetLoader.Load(path);

        // Assert
        load.Should().Throw<FileNotFoundException>()
            .Where(exception => exception.FileName == path && exception.Message.Contains(path));
    }
}
