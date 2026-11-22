using FluentAssertions;
using InternHub.Api.Features.Auth.GetCurrentUser;
using InternHub.Api.Features.Shared;

namespace InternHub.Tests.Unit.Auth;

public sealed class GetCurrentUserHandlerTests
{
    [Fact]
    public async Task Given_StudentClaims_When_Handling_Then_ReturnsStudentIdentity()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var handler = new GetCurrentUserHandler();
        var query = new GetCurrentUserQuery(
            accountId.ToString("D"),
            "Student",
            studentId.ToString("D"),
            null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(
            new GetCurrentUserResponse(accountId, "Student", studentId, null));
    }

    [Fact]
    public async Task Given_RecruiterClaims_When_Handling_Then_ReturnsRecruiterIdentity()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var recruiterId = Guid.NewGuid();
        var handler = new GetCurrentUserHandler();
        var query = new GetCurrentUserQuery(
            accountId.ToString("D"),
            "Recruiter",
            null,
            recruiterId.ToString("D"));

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(
            new GetCurrentUserResponse(accountId, "Recruiter", null, recruiterId));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-guid")]
    public async Task Given_MissingOrInvalidSubject_When_Handling_Then_ReturnsUnauthorized(
        string? subject)
    {
        // Arrange
        var handler = new GetCurrentUserHandler();
        var query = new GetCurrentUserQuery(subject, "Student", null, null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Given_MissingRole_When_Handling_Then_ReturnsUnauthorized(string? role)
    {
        // Arrange
        var handler = new GetCurrentUserHandler();
        var query = new GetCurrentUserQuery(Guid.NewGuid().ToString("D"), role, null, null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Given_MalformedOptionalAccountClaim_When_Handling_Then_ReturnsUnauthorized()
    {
        // Arrange
        var handler = new GetCurrentUserHandler();
        var query = new GetCurrentUserQuery(
            Guid.NewGuid().ToString("D"),
            "Student",
            "not-a-guid",
            null);

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Given_MalformedRecruiterClaim_When_Handling_Then_ReturnsUnauthorized()
    {
        // Arrange
        var handler = new GetCurrentUserHandler();
        var query = new GetCurrentUserQuery(
            Guid.NewGuid().ToString("D"),
            "Recruiter",
            null,
            "not-a-guid");

        // Act
        var result = await handler.HandleAsync(query, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Unauthorized);
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Given_CurrentUserResponse_When_InspectingPublicFields_Then_ExposesNoSensitiveData()
    {
        // Arrange
        var responseProperties = typeof(GetCurrentUserResponse)
            .GetProperties()
            .Select(property => property.Name);

        // Act
        var propertyNames = responseProperties.ToArray();

        // Assert
        propertyNames.Should().BeEquivalentTo(
            "AccountId",
            "Role",
            "StudentId",
            "RecruiterId");
    }
}
