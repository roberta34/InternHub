using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;

namespace InternHub.Tests.Unit.InternshipCandidacies.RecruiterAccess;

public sealed class ClaimsPrincipalRecruiterAccessGuardTests
{
    private const string AuthenticationType = "Test";

    [Fact]
    public async Task Given_NullUser_When_Resolving_Then_ReturnsUnauthorized()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        ClaimsPrincipal? user = null;

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Unauthorized);
    }

    [Fact]
    public async Task Given_AnonymousUser_When_Resolving_Then_ReturnsUnauthorized()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Unauthorized);
    }

    [Fact]
    public async Task Given_Student_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(
            new Claim("role", "Student"),
            new Claim("student_id", Guid.NewGuid().ToString()));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_RecruiterWithoutRecruiterId_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(new Claim("role", "Recruiter"));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Theory]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("")]
    public async Task Given_RecruiterWithInvalidRecruiterId_When_Resolving_Then_ReturnsForbidden(string recruiterId)
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(
            new Claim("role", "Recruiter"),
            new Claim("recruiter_id", recruiterId));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_RecruiterWithTwoRecruiterIds_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(
            new Claim("role", "Recruiter"),
            new Claim("recruiter_id", Guid.NewGuid().ToString()),
            new Claim("recruiter_id", Guid.NewGuid().ToString()));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_RecruiterWithStudentIdClaim_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(
            new Claim("role", "Recruiter"),
            new Claim("recruiter_id", Guid.NewGuid().ToString()),
            new Claim("student_id", Guid.NewGuid().ToString()));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_RecruiterAndStudentRoles_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(
            new Claim("role", "Recruiter"),
            new Claim("role", "Student"),
            new Claim("recruiter_id", Guid.NewGuid().ToString()));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_SubjectDifferentFromNameIdentifier_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var user = Principal(
            new Claim("role", "Recruiter"),
            new Claim("recruiter_id", Guid.NewGuid().ToString()),
            new Claim("sub", Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_SecondAuthenticatedIdentityWithIdentifiers_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var recruiter = new ClaimsIdentity(
            [new Claim("role", "Recruiter"), new Claim("recruiter_id", Guid.NewGuid().ToString())],
            AuthenticationType);
        var other = new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString())],
            AuthenticationType);
        var user = new ClaimsPrincipal([recruiter, other]);

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Forbidden);
    }

    [Theory]
    [InlineData("role")]
    [InlineData(ClaimTypes.Role)]
    public async Task Given_ValidRecruiter_When_Resolving_Then_ReturnsAuthorizedWithRecruiterId(string roleClaimType)
    {
        // Arrange
        var guard = new ClaimsPrincipalRecruiterAccessGuard();
        var recruiterId = Guid.NewGuid();
        var userId = Guid.NewGuid().ToString("D");
        var user = Principal(
            new Claim(roleClaimType, "Recruiter"),
            new Claim("recruiter_id", recruiterId.ToString("D")),
            new Claim("sub", userId),
            new Claim(ClaimTypes.NameIdentifier, userId));

        // Act
        var result = await guard.ResolveAuthenticatedRecruiterAsync(user);

        // Assert
        result.Status.Should().Be(RecruiterAccessStatus.Authorized);
        result.RecruiterId.Should().Be(recruiterId);
    }

    private static ClaimsPrincipal Principal(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, AuthenticationType));
}
