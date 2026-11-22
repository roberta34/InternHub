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

    // -------------------------------------------------------------------------
    // 1. Basic matching
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Given_CvWithMultipleExpandedVocabularySkills_When_Analyzing_Then_MatchesExactCanonicalSkills()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Built services using TypeScript, PostgreSQL, Linux, Kubernetes, Azure, AWS, and CI/CD.",
            "TypeScript, PostgreSQL, Kubernetes, Linux, Azure, AWS, CI/CD, GraphQL");

        // Assert
        result.MatchedSkills.Should().Equal("TypeScript", "PostgreSQL", "Linux", "Kubernetes", "Azure", "AWS", "CI/CD");
        result.MissingSkills.Should().Equal("GraphQL");
    }

    [Theory]
    [InlineData(".net", ".NET")]
    [InlineData("c#", "C#")]
    [InlineData("c++", "C++")]
    [InlineData("sql server", "SQL Server")]
    [InlineData("ASP.NET CORE", "ASP.NET Core")]
    [InlineData("ef core", "Entity Framework Core")]
    [InlineData("ci/cd", "CI/CD")]
    [InlineData("k8s", "Kubernetes")]
    public async Task Given_SkillOrAliasInVaryingCasing_When_Analyzing_Then_MatchesCaseInsensitively(
        string cvMention,
        string expectedCanonicalSkill)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync($"Experience with {cvMention}.", expectedCanonicalSkill);

        // Assert
        result.MatchedSkills.Should().Equal(expectedCanonicalSkill);
        result.MissingSkills.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Python3", "Python")]
    [InlineData("Python 3", "Python")]
    [InlineData("JS", "JavaScript")]
    [InlineData("Java Script", "JavaScript")]
    [InlineData("ECMAScript", "JavaScript")]
    [InlineData("CSharp", "C#")]
    [InlineData("C Sharp", "C#")]
    [InlineData("C-Sharp", "C#")]
    [InlineData("CPP", "C++")]
    [InlineData("DotNet", ".NET")]
    [InlineData("Dot Net", ".NET")]
    [InlineData(".NET Core", ".NET")]
    [InlineData("ReactJS", "React")]
    [InlineData("React.js", "React")]
    [InlineData("SQLServer", "SQL Server")]
    [InlineData("MS SQL Server", "SQL Server")]
    [InlineData("Microsoft SQL Server", "SQL Server")]
    [InlineData("MS SQL", "SQL Server")]
    [InlineData("MSSQL", "SQL Server")]
    [InlineData("TS", "TypeScript")]
    [InlineData("Type Script", "TypeScript")]
    [InlineData("Postgres", "PostgreSQL")]
    [InlineData("Postgre SQL", "PostgreSQL")]
    [InlineData("No SQL", "NoSQL")]
    [InlineData("Mongo", "MongoDB")]
    [InlineData("ASP.NET", "ASP.NET Core")]
    [InlineData("ASPNET Core", "ASP.NET Core")]
    [InlineData("EF Core", "Entity Framework Core")]
    [InlineData("Entity Framework", "Entity Framework Core")]
    [InlineData("NodeJS", "Node.js")]
    [InlineData("Node JS", "Node.js")]
    [InlineData("AngularJS", "Angular")]
    [InlineData("VueJS", "Vue.js")]
    [InlineData("Vue", "Vue.js")]
    [InlineData("HTML5", "HTML")]
    [InlineData("CSS3", "CSS")]
    [InlineData("RESTful API", "REST API")]
    [InlineData("REST APIs", "REST API")]
    [InlineData("RESTful APIs", "REST API")]
    [InlineData("RESTful", "REST API")]
    [InlineData("Unit Tests", "Unit Testing")]
    [InlineData("Unit Test", "Unit Testing")]
    [InlineData("Unit-Testing", "Unit Testing")]
    [InlineData("K8s", "Kubernetes")]
    [InlineData("Kube", "Kubernetes")]
    [InlineData("Microsoft Azure", "Azure")]
    [InlineData("Amazon Web Services", "AWS")]
    [InlineData("CICD", "CI/CD")]
    [InlineData("Docker-Compose", "Docker Compose")]
    public async Task Given_ExplicitAliasInCv_When_AnalyzingCanonicalRequirement_Then_MatchesCanonicalSkill(
        string aliasInCv,
        string canonicalRequirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            $"Hands-on experience with {aliasInCv} in academic projects.",
            $"Required skill: {canonicalRequirement}.");

        // Assert
        result.MatchedSkills.Should().Equal(canonicalRequirement);
        result.MissingSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_ExplicitAliasesInRequirements_When_Analyzing_Then_NormalizesToCanonicalSkillNames()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Built APIs with C#, .NET, and PostgreSQL.",
            "Looking for CSharp, DotNet, Postgres, and K8s.");

        // Assert
        result.MatchedSkills.Should().Equal("C#", ".NET", "PostgreSQL");
        result.MissingSkills.Should().Equal("Kubernetes");
    }

    [Fact]
    public async Task Given_DuplicateAliasesAndRepeatedMentions_When_Analyzing_Then_DeduplicatesMatchedAndMissingSkills()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "C#, CSharp, C Sharp, C-Sharp, .NET, DotNet, .NET Core, and JS / JavaScript.",
            "CSharp, C#, C-Sharp, DotNet, .NET, JS, ECMAScript, K8s, Kubernetes, Kube");

        // Assert
        result.MatchedSkills.Should().Equal("JavaScript", "C#", ".NET");
        result.MissingSkills.Should().Equal("Kubernetes");
        result.Suggestions.Should().HaveCount(1);
    }

    [Fact]
    public async Task Given_SameSkillsInDifferentOrders_When_Analyzing_Then_ProducesDeterministicCanonicalOrder()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var firstResult = await service.AnalyzeAsync(
            "Git, Docker, .NET, C#, Python",
            "Kubernetes, Git, Docker, .NET, C#, Python, React");
        var secondResult = await service.AnalyzeAsync(
            "Python, C#, .NET, Docker, Git",
            "React, Python, C#, .NET, Docker, Git, Kubernetes");

        // Assert
        firstResult.MatchedSkills.Should().Equal("Python", "C#", ".NET", "Docker", "Git");
        firstResult.MissingSkills.Should().Equal("React", "Kubernetes");
        secondResult.Should().BeEquivalentTo(firstResult, options => options.WithStrictOrdering());
    }

    // -------------------------------------------------------------------------
    // 2. False-positive prevention
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("I build frontend apps with JavaScript.")]
    [InlineData("I build frontend apps with Java Script.")]
    public async Task Given_JavaScriptCv_When_RequirementIsOnlyJava_Then_DoesNotFalsePositiveMatchJava(
        string cvContent)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, "Java");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("Java");
    }

    [Theory]
    [InlineData("I write systems code in C++.", "C#")]
    [InlineData("I write backend code in C#.", "C++")]
    [InlineData("Experimental syntax like C## and C#++.", "C#")]
    [InlineData("Experimental syntax like C+++.", "C++")]
    public async Task Given_CSharpAndCppBoundaryCases_When_Analyzing_Then_PreventsCrossMatchingAndSymbolBleeding(
        string cvContent,
        string requirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, requirement);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(requirement);
    }

    [Theory]
    [InlineData("Administered SQL Server databases.", "SQL")]
    [InlineData("Used Microsoft SQL Server and MS SQL.", "SQL")]
    [InlineData("Wrote queries in SQL.", "SQL Server")]
    public async Task Given_SqlAndSqlServer_When_Analyzing_Then_DoesNotCrossMatch(
        string cvContent,
        string requirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, requirement);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(requirement);
    }

    [Fact]
    public async Task Given_SqlAndSqlServerBothPresent_When_Analyzing_Then_MatchesBothDistinctly()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Designed schemas in SQL Server and wrote portable SQL queries.",
            "SQL, SQL Server");

        // Assert
        result.MatchedSkills.Should().Equal("SQL", "SQL Server");
        result.MissingSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_GitHubOnlyInCv_When_RequirementIsGit_Then_DoesNotMatchGit()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Hosted my portfolio on GitHub and GitLab.",
            "Git");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("Git");
    }

    [Theory]
    [InlineData("Developed web APIs with ASP.NET Core.", ".NET")]
    [InlineData("Built web forms with ASP.NET.", ".NET")]
    [InlineData("Built console utilities with .NET.", "ASP.NET Core")]
    public async Task Given_DotNetAndAspNetCore_When_Analyzing_Then_DistinguishesTheSkills(
        string cvContent,
        string requirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, requirement);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(requirement);
    }

    [Theory]
    [InlineData("Defined local stacks with Docker Compose.", "Docker")]
    [InlineData("Configured services using docker-compose.", "Docker")]
    [InlineData("Packaged apps with Docker.", "Docker Compose")]
    public async Task Given_DockerAndDockerCompose_When_Analyzing_Then_DistinguishesTheSkills(
        string cvContent,
        string requirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, requirement);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(requirement);
    }

    [Theory]
    [InlineData("Experienced with NoSQL document stores.")]
    [InlineData("Experienced with No SQL databases.")]
    public async Task Given_NoSqlInCv_When_RequirementIsSql_Then_DoesNotMatchSql(string cvContent)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, "SQL");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("SQL");
    }

    [Theory]
    [InlineData("Edited app.js during an internship.", "JavaScript")]
    [InlineData("Edited index.ts for a small script.", "TypeScript")]
    [InlineData("Reviewed schema.sql backup files.", "SQL")]
    [InlineData("Updated index.html and styles.css.", "HTML")]
    [InlineData("Updated index.html and styles.css.", "CSS")]
    [InlineData("Published docs at example.net.", ".NET")]
    [InlineData("Visited https://python.org for reading.", "Python")]
    [InlineData("Checked internal docs at git.example.com.", "Git")]
    [InlineData("Read notes in sql.txt.", "SQL")]
    public async Task Given_SkillEmbeddedInFilenameOrDomainName_When_Analyzing_Then_DoesNotFalsePositiveMatch(
        string cvContent,
        string requirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, requirement);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(requirement);
    }

    [Fact]
    public async Task Given_OrdinaryEnglishWordRestInCv_When_RequirementIsRestApi_Then_DoesNotFalsePositiveMatch()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "I built Python scripts and spent the rest of the summer writing documentation.",
            "Python, REST API");

        // Assert
        result.MatchedSkills.Should().Equal("Python");
        result.MissingSkills.Should().Equal("REST API");
    }

    [Fact]
    public async Task Given_OrdinaryEnglishWordRestInRequirements_When_Analyzing_Then_DoesNotExtractRestApiRequirement()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Experienced with Python.",
            "Work with the rest of the engineering team using Python.");

        // Assert
        result.MatchedSkills.Should().Equal("Python");
        result.MissingSkills.Should().BeEmpty();
    }

    [Theory]
    [InlineData("Managed project assets, adjustments, and reports.", "TypeScript")]
    [InlineData("Managed project assets, adjustments, and reports.", "JavaScript")]
    [InlineData("Worked on visual design reviews.", "Vue.js")]
    [InlineData("Collaborated on reactive chemistry simulations.", "React")]
    public async Task Given_OrdinaryEnglishWordsContainingSkillSubstrings_When_Analyzing_Then_DoesNotMatch(
        string cvContent,
        string requirement)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, requirement);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(requirement);
    }

    // -------------------------------------------------------------------------
    // 3. Compound and punctuation-sensitive skills
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("Developed services in .NET (version 10).", ".NET", ".NET")]
    [InlineData("Implemented backend logic in C#/C++.", "C#", "C#")]
    [InlineData("Implemented backend logic in C#/C++.", "C++", "C++")]
    [InlineData("Built APIs with ASP.NET   Core.", "ASP.NET Core", "ASP.NET Core")]
    [InlineData("Persisted entities using Entity   Framework\tCore.", "Entity Framework Core", "Entity Framework Core")]
    [InlineData("Started dependencies with Docker   Compose.", "Docker Compose", "Docker Compose")]
    [InlineData("Created browser widgets in Java   Script.", "JavaScript", "JavaScript")]
    [InlineData("Queried data stored in Postgre   SQL.", "PostgreSQL", "PostgreSQL")]
    public async Task Given_CompoundOrPunctuationSensitiveSkill_When_Analyzing_Then_MatchesAccurately(
        string cvContent,
        string offerRequirements,
        string expectedSkill)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, offerRequirements);

        // Assert
        result.MatchedSkills.Should().Equal(expectedSkill);
        result.MissingSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_PrefixedDotBeforeDotNet_When_Analyzing_Then_DoesNotMatchDotNet()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("Relative path ..NET is not a framework.", ".NET");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal(".NET");
    }

    [Fact]
    public async Task Given_PostgreSqlSpacedAliasInCv_When_RequirementIsSql_Then_DoesNotFalsePositiveMatchSql()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("Configured Postgre SQL locally.", "SQL");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("SQL");
    }

    // -------------------------------------------------------------------------
    // 4. Requirements and CV edge cases
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   \t\r\n  ")]
    [InlineData("Strong communication, teamwork, and curiosity.")]
    public async Task Given_EmptyWhitespaceOrUnrecognizedRequirements_When_Analyzing_Then_ReturnsEmptyCollectionsAndInformativeSummary(
        string offerRequirements)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync("Python, C#, .NET, Docker", offerRequirements);

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().BeEmpty();
        result.Suggestions.Should().BeEmpty();
        result.Summary.Should().Be("No recognized skills were found in the internship requirements.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   \t\r\n  ")]
    public async Task Given_EmptyOrWhitespaceCv_When_Analyzing_Then_MarksAllRequiredSkillsMissing(
        string cvContent)
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(cvContent, "C#, .NET, Docker");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("C#", ".NET", "Docker");
        result.Suggestions.Should().HaveCount(3);
        result.Summary.Should().Be("Matched 0 of 3 recognized required skill(s); 3 skill(s) are missing.");
    }

    [Fact]
    public async Task Given_CvWithNoneOfTheRequiredSkills_When_Analyzing_Then_ReportsZeroMatchedAndAllMissing()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Experienced with Java, Angular, and AWS.",
            "Python, Docker, Kubernetes");

        // Assert
        result.MatchedSkills.Should().BeEmpty();
        result.MissingSkills.Should().Equal("Python", "Docker", "Kubernetes");
        result.Suggestions.Should().HaveCount(3);
        result.Summary.Should().Be("Matched 0 of 3 recognized required skill(s); 3 skill(s) are missing.");
    }

    [Fact]
    public async Task Given_CvWithAllRequiredSkills_When_Analyzing_Then_ReturnsEmptyMissingAndSuggestions()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Projects using CSharp, .NET Core, EF Core, and Git.",
            "C#, .NET, Entity Framework Core, Git");

        // Assert
        result.MatchedSkills.Should().Equal("C#", ".NET", "Git", "Entity Framework Core");
        result.MissingSkills.Should().BeEmpty();
        result.Suggestions.Should().BeEmpty();
        result.Summary.Should().Be("Matched all 4 recognized required skill(s).");
    }

    [Fact]
    public async Task Given_CvWithPartialRequiredSkills_When_Analyzing_Then_SeparatesMatchedAndMissingAccurately()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Worked with C#, SQL Server, and Git.",
            "C#, .NET, SQL Server, Docker, Git");

        // Assert
        result.MatchedSkills.Should().Equal("C#", "Git", "SQL Server");
        result.MissingSkills.Should().Equal(".NET", "Docker");
        result.Suggestions.Should().HaveCount(2);
        result.Summary.Should().Be("Matched 3 of 5 recognized required skill(s); 2 skill(s) are missing.");
    }

    // -------------------------------------------------------------------------
    // 5. Result correctness
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Given_PartialMatch_When_Analyzing_Then_SuggestionsCorrespondOnlyToMissingSkillsInOrder()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var result = await service.AnalyzeAsync(
            "Strong experience with Python and Git.",
            "Python, C#, Docker, Git, Kubernetes");

        // Assert
        result.MatchedSkills.Should().Equal("Python", "Git");
        result.MissingSkills.Should().Equal("C#", "Docker", "Kubernetes");
        result.MatchedSkills.Should().NotIntersectWith(result.MissingSkills);
        result.Suggestions.Should().HaveCount(result.MissingSkills.Count);

        for (var i = 0; i < result.MissingSkills.Count; i++)
        {
            result.Suggestions[i].Should().Contain(result.MissingSkills[i]);
        }

        result.Suggestions.Should().NotContain(s => s.Contains("Python", StringComparison.OrdinalIgnoreCase));
        result.Suggestions.Should().NotContain(s => s.Contains("Git", StringComparison.OrdinalIgnoreCase));
        result.Summary.Should().Be("Matched 2 of 5 recognized required skill(s); 3 skill(s) are missing.");
    }

    [Fact]
    public async Task Given_RepeatedInvocationsOnComplexInput_When_Analyzing_Then_ProducesIdenticalDeterministicOutput()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();
        const string cv = "CSharp, DotNet, ASP.NET Core, Postgres, Docker Compose, Unit Tests";
        const string requirements = "C#, .NET, ASP.NET Core, PostgreSQL, Docker, Docker Compose, Unit Testing, Azure";

        // Act
        var results = await Task.WhenAll(
            Enumerable.Range(0, 5).Select(_ => service.AnalyzeAsync(cv, requirements)));

        // Assert
        foreach (var result in results)
        {
            result.Should().BeEquivalentTo(results[0], options => options.WithStrictOrdering());
        }
    }

    // -------------------------------------------------------------------------
    // 6. Input and cancellation behavior
    // -------------------------------------------------------------------------

    [Fact]
    public async Task Given_BothInputsNull_When_Analyzing_Then_ThrowsArgumentNullExceptionForCvContentFirst()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var action = () => service.AnalyzeAsync(null!, null!);

        // Assert
        await action.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("cvContent");
    }

    [Fact]
    public async Task Given_AlreadyCanceledTokenWithEmptyInputs_When_Analyzing_Then_ThrowsOperationCanceledException()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();
        using var cancellationSource = new CancellationTokenSource();
        cancellationSource.Cancel();

        // Act
        var action = () => service.AnalyzeAsync(string.Empty, string.Empty, cancellationSource.Token);

        // Assert
        await action.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Given_ValidInputs_When_Analyzing_Then_CompletesSynchronouslyWithoutArtificialAsyncWork()
    {
        // Arrange
        var service = new KeywordRecruiterEvaluationService();

        // Act
        var task = service.AnalyzeAsync("C# and .NET", "C#, .NET, Docker");
        var completedSynchronously = task.IsCompletedSuccessfully;
        var result = await task;

        // Assert
        completedSynchronously.Should().BeTrue();
        result.MatchedSkills.Should().Equal("C#", ".NET");
        result.MissingSkills.Should().Equal("Docker");
    }
}
