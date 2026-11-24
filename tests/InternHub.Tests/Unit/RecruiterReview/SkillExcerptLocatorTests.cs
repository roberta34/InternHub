using FluentAssertions;
using InternHub.Api.Features.RecruiterReview;

namespace InternHub.Tests.Unit.RecruiterReview;

public sealed class SkillExcerptLocatorTests
{
    [Theory]
    [InlineData("Java", "Frontend work in JavaScript.")]
    [InlineData("C", "Backend work in C#.")]
    [InlineData("C", "Systems work in C++.")]
    [InlineData("C#", "Embedded work in C.")]
    [InlineData("SQL", "Administered SQL Server databases.")]
    [InlineData("SQL Server", "Wrote SQL queries.")]
    public void Given_SkillOnlyInsideDifferentTerm_When_Locating_Then_SkillIsNotFound(string skill, string cv)
    {
        // Arrange
        string[] skills = [skill];

        // Act
        var excerpts = SkillExcerptLocator.Locate(cv, skills);

        // Assert
        excerpts.Should().BeEmpty();
    }

    [Theory]
    [InlineData("c#", "Worked with C#.")]
    [InlineData("Java", "Languages: Java, JavaScript.")]
    [InlineData("SQL", "Used SQL Server and plain SQL.")]
    [InlineData(".NET", "Built .NET services.")]
    [InlineData("Node.js", "Used Node.js daily.")]
    public void Given_SkillAsWholeTerm_When_Locating_Then_SkillIsFound(string skill, string cv)
    {
        // Arrange
        string[] skills = [skill];

        // Act
        var excerpts = SkillExcerptLocator.Locate(cv, skills);

        // Assert
        excerpts.Should().ContainKey(skill);
    }

    [Fact]
    public void Given_BothCompoundAndShortSkillInCv_When_Locating_Then_FindsEachInItsOwnPlace()
    {
        // Arrange
        string[] skills = ["SQL", "SQL Server"];

        // Act
        var excerpts = SkillExcerptLocator.Locate("Tuned SQL Server. Wrote SQL by hand.", skills);

        // Assert
        excerpts.Keys.Should().BeEquivalentTo("SQL", "SQL Server");
        excerpts["SQL"].Should().Contain("Wrote SQL");
    }

    [Fact]
    public void Given_SkillInLongCv_When_Locating_Then_ExcerptContainsSkillAndIsBounded()
    {
        // Arrange
        var cv = new string('x', 100) + " Docker " + new string('y', 100);
        string[] skills = ["Docker"];

        // Act
        var excerpts = SkillExcerptLocator.Locate(cv, skills);

        // Assert
        excerpts["Docker"].Should().Contain("Docker");
        excerpts["Docker"].Length.Should().BeLessThanOrEqualTo("Docker".Length + 2 * SkillExcerptLocator.ExcerptRadius);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Given_NullOrEmptyCv_When_Locating_Then_FindsNothing(string? cv)
    {
        // Arrange
        string[] skills = ["C#"];

        // Act
        var excerpts = SkillExcerptLocator.Locate(cv, skills);

        // Assert
        excerpts.Should().BeEmpty();
    }
}
