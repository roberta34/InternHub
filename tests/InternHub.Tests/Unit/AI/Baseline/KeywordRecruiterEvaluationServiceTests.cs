using FluentAssertions;
using InternHub.Api.Infrastructure.AI.Baseline;

namespace InternHub.Tests.Unit.AI.Baseline;

public sealed class KeywordRecruiterEvaluationServiceTests
{
    [Fact]
    public async Task Given_CVWithSomeRequiredSkills_When_Analyzing_Then_ReturnsMatchedAndMissingSkills()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("I know Python and SQL.", "Python, SQL, Docker");

        // Assert
        result.MatchedSkills.Should().Equal("Python", "SQL");
        result.MissingSkills.Should().Equal("Docker");
    }

    [Fact]
    public async Task Given_JavaAndJavaScript_When_Analyzing_Then_DistinguishesTheSkills()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("I know JavaScript.", "Java, JavaScript");

        // Assert
        result.MatchedSkills.Should().Equal("JavaScript");
        result.MissingSkills.Should().Equal("Java");
    }

    [Fact]
    public async Task Given_Java_When_AnalyzingJavaScriptRequirement_Then_DoesNotMatchJavaScript()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("I know Java.", "JavaScript");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("JavaScript");
    }

    [Fact]
    public async Task Given_CSharpAndCpp_When_Analyzing_Then_DistinguishesSpecialCharacterSkills()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("I know C#.", "C#, C++");

        // Assert
        result.MatchedSkills.Should().Equal("C#");
        result.MissingSkills.Should().Equal("C++");
    }

    [Fact]
    public async Task Given_DotNetAndJavaScriptRequirements_When_Analyzing_Then_MatchesRecognizedSkillsExactly()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "I developed web applications with .NET and JavaScript.",
            "Experience with .NET and JavaScript is required.");

        // Assert
        result.MatchedSkills.Should().Equal("JavaScript", ".NET");
        result.MissingSkills.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Python")]
    [InlineData("PYTHON")]
    [InlineData("python")]
    public async Task Given_PythonInDifferentCasing_When_Analyzing_Then_RecognizesTheSkill(string cvSkill)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvSkill, "Python");

        // Assert
        result.MatchedSkills.Should().Equal("Python");
        result.MissingSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_DuplicateSkillMentions_When_Analyzing_Then_ReturnsEachSkillOnce()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Python, python and PYTHON are skills I use.",
            "Python, Python, python");

        // Assert
        result.MatchedSkills.Should().Equal("Python");
        result.MissingSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_EmptyCV_When_Analyzing_Then_ReportsRecognizedRequirementsAsMissing()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(string.Empty, "Python, SQL, Docker");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("Python", "SQL", "Docker");
    }

    [Fact]
    public async Task Given_EmptyRequirements_When_Analyzing_Then_ReturnsEmptySkillListsAndInformativeSummary()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("Python and SQL", string.Empty);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().BeEmpty();
        result.Summary.Should().Contain("No recognized skills");
    }

    [Fact]
    public async Task Given_NullCvContent_When_Analyzing_Then_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var action = () => service.AnalyzeAsync(null!, "Python");

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("cvContent");
    }

    [Fact]
    public async Task Given_NullOfferRequirements_When_Analyzing_Then_ThrowsArgumentNullException()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var action = () => service.AnalyzeAsync("Python", null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("offerRequirements");
    }

    [Fact]
    public async Task Given_SameInputTwice_When_Analyzing_Then_ReturnsIdenticalResults()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();
        const string cvContent = "Python, SQL and Docker";
        const string offerRequirements = "Python, SQL, Docker, React";

        // Act
        var firstResult = await service.AnalyzeAsync(cvContent, offerRequirements);
        var secondResult = await service.AnalyzeAsync(cvContent, offerRequirements);

        // Assert
        secondResult.Should().BeEquivalentTo(firstResult);
    }

    [Fact]
    public async Task Given_MissingSkills_When_Analyzing_Then_ReturnsCoherentSuggestionsAndSummary()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("Python", "Python, Docker, React");

        // Assert
        result.Suggestions.Should().HaveCount(result.MissingSkills.Count);
        result.Suggestions.Should().Contain(suggestion => suggestion.Contains("Docker"));
        result.Suggestions.Should().Contain(suggestion => suggestion.Contains("React"));
        result.Summary.Should().Contain("Matched 1 of 3");
        result.Summary.Should().Contain("2 skill(s) are missing");
    }

    [Fact]
    public async Task Given_AlreadyCanceledToken_When_Analyzing_Then_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        var action = () => service.AnalyzeAsync("Python", "Python", cancellationSource.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Given_SqlInsideLongerWord_When_Analyzing_Then_DoesNotMatchSql()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("I worked with SQLServer.", "SQL");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("SQL");
    }
}
