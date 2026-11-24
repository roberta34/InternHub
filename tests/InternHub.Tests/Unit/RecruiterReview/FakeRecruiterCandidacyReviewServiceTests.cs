using FluentAssertions;
using InternHub.Api.Features.RecruiterReview;
using InternHub.Tests.Unit.RecruiterReview.Fakes;

namespace InternHub.Tests.Unit.RecruiterReview;

public sealed class FakeRecruiterCandidacyReviewServiceTests
{
    [Fact]
    public async Task Given_SameInput_When_ReviewingTwice_Then_ReturnsEquivalentResults()
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();
        const string requirements = "C#, SQL, Docker";
        const string cv = "Built REST APIs in C# backed by SQL.";

        // Act
        var first = await service.ReviewAsync(requirements, cv);
        var second = await service.ReviewAsync(requirements, cv);

        // Assert
        second.Should().BeEquivalentTo(first, options => options.WithStrictOrdering());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Given_EmptyOrWhitespaceCv_When_Reviewing_Then_AllRequiredSkillsAreMissingAndNoEvidence(string cv)
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();

        // Act
        var review = await service.ReviewAsync("C#, SQL", cv);

        // Assert
        review.MatchedSkills.Should().BeEmpty();
        review.MissingOrUnclearSkills.Should().Equal("C#", "SQL");
        review.Evidence.Should().BeEmpty();
        review.Summary.Should().Be("Matched 0 of 2 required skills.");
    }

    [Fact]
    public async Task Given_EmptyRequirements_When_Reviewing_Then_ReturnsEmptyListsAndNoEvidence()
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();

        // Act
        var review = await service.ReviewAsync("   ", "Built REST APIs in C#.");

        // Assert
        review.MatchedSkills.Should().BeEmpty();
        review.MissingOrUnclearSkills.Should().BeEmpty();
        review.Evidence.Should().BeEmpty();
        review.Summary.Should().Be("Matched 0 of 0 required skills.");
    }

    [Theory]
    [InlineData("Java", "Frontend work in JavaScript.")]
    [InlineData("C", "Backend work in C#.")]
    [InlineData("C#", "Embedded work in C.")]
    [InlineData("SQL", "Administered SQL Server databases.")]
    [InlineData("SQL Server", "Wrote SQL queries.")]
    public async Task Given_SkillOnlyInsideDifferentTerm_When_Reviewing_Then_SkillIsMissing(
        string requiredSkill,
        string cv)
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();

        // Act
        var review = await service.ReviewAsync(requiredSkill, cv);

        // Assert
        review.MatchedSkills.Should().BeEmpty();
        review.MissingOrUnclearSkills.Should().Equal(requiredSkill);
        review.Evidence.Should().BeEmpty();
    }

    [Theory]
    [InlineData("c#", "Worked with C#.")]
    [InlineData("Java", "Languages: Java, JavaScript.")]
    [InlineData("SQL", "Used SQL Server and plain SQL.")]
    public async Task Given_SkillAsWholeTermWithDifferentCaseOrPunctuation_When_Reviewing_Then_SkillIsMatched(
        string requiredSkill,
        string cv)
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();

        // Act
        var review = await service.ReviewAsync(requiredSkill, cv);

        // Assert
        review.MatchedSkills.Should().Equal(requiredSkill);
        review.MissingOrUnclearSkills.Should().BeEmpty();
    }

    [Fact]
    public async Task Given_MatchedAndMissingSkills_When_Reviewing_Then_EvidenceOnlyForMatchedSkillsWithExcerptContainingSkill()
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();

        // Act
        var review = await service.ReviewAsync(
            "C#; Docker\nSQL",
            "Built REST APIs in C# and stored data with SQL.");

        // Assert
        review.MatchedSkills.Should().Equal("C#", "SQL");
        review.MissingOrUnclearSkills.Should().Equal("Docker");
        review.Evidence.Select(item => item.Skill).Should().Equal("C#", "SQL");
        review.Evidence[0].Excerpt.Should().Contain("C#");
        review.Evidence[1].Excerpt.Should().Contain("SQL");
        review.Summary.Should().Be("Matched 2 of 3 required skills.");
    }

    [Fact]
    public async Task Given_UnavailableMode_When_Reviewing_Then_ThrowsProviderExceptionAndCountsCall()
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService { SimulateUnavailable = true };

        // Act
        var act = () => service.ReviewAsync("C#", "Built REST APIs in C#.");

        // Assert
        await act.Should().ThrowAsync<RecruiterCandidacyReviewProviderException>();
        service.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_ThreeReviews_When_Counting_Then_CallCountIsThree()
    {
        // Arrange
        var service = new FakeRecruiterCandidacyReviewService();

        // Act
        await service.ReviewAsync("C#", "C#");
        await service.ReviewAsync("C#", "C#");
        await service.ReviewAsync("C#", "C#");

        // Assert
        service.CallCount.Should().Be(3);
    }
}
