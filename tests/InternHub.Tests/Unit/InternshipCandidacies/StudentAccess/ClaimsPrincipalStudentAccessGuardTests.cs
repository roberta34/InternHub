using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;

namespace InternHub.Tests.Unit.InternshipCandidacies.StudentAccess;

public sealed class ClaimsPrincipalStudentAccessGuardTests
{
    private const string StudentIdClaimType = "student_id";
    private const string RecruiterIdClaimType = "recruiter_id";
    private const string SubjectClaimType = "sub";

    [Fact]
    public async Task Given_ValidStudentWithDistinctAccountSubAndStudentId_When_AuthorizingOwnStudentId_Then_ReturnsAuthorized()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        const string accountId = "account-student-42";
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = CreateStudentPrincipal(
            studentId.ToString(),
            sub: accountId,
            nameIdentifier: accountId);

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Authorized);
        result.StudentId.Should().Be(studentId);
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task Given_ValidStudent_When_ResolvingAuthenticatedStudent_Then_ReturnsAuthorizedWithStudentId()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = CreateStudentPrincipal(
            studentId.ToString(),
            sub: Guid.NewGuid().ToString());

        // Act
        var result = await guard.ResolveAuthenticatedStudentAsync(principal);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Authorized);
        result.StudentId.Should().Be(studentId);
    }

    [Fact]
    public async Task Given_NullOrUnauthenticatedPrincipal_When_Authorizing_Then_ReturnsUnauthorized()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity());

        // Act
        var nullResult = await guard.AuthorizeStudentAccessAsync(null, studentId);
        var unauthenticatedResult = await guard.AuthorizeStudentAccessAsync(unauthenticated, studentId);

        // Assert
        nullResult.Status.Should().Be(StudentAccessStatus.Unauthorized);
        nullResult.StudentId.Should().BeNull();
        unauthenticatedResult.Status.Should().Be(StudentAccessStatus.Unauthorized);
        unauthenticatedResult.StudentId.Should().BeNull();
    }

    [Fact]
    public async Task Given_StudentA_When_AuthorizingStudentB_Then_ReturnsForbidden()
    {
        // Arrange
        var studentA = Guid.NewGuid();
        var studentB = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principalA = CreateStudentPrincipal(studentA.ToString(), sub: "account-a");

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principalA, studentB);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
        result.StudentId.Should().BeNull();
    }

    [Fact]
    public async Task Given_AuthenticatedRecruiter_When_AuthorizingStudent_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var recruiterPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Recruiter"),
                    new Claim(RecruiterIdClaimType, Guid.NewGuid().ToString()),
                    new Claim(SubjectClaimType, "account-recruiter-1")
                ],
                authenticationType: "TestScheme"));

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(recruiterPrincipal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
        result.StudentId.Should().BeNull();
    }

    [Fact]
    public async Task Given_StudentWithAccountSubEqualToRouteStudentIdButMissingStudentIdClaim_When_Authorizing_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = CreateStudentPrincipal(
            studentId: null,
            sub: studentId.ToString());

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
        result.StudentId.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-guid")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    public async Task Given_InvalidStudentIdClaim_When_ResolvingAuthenticatedStudent_Then_ReturnsForbidden(
        string? invalidStudentId)
    {
        // Arrange
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = CreateStudentPrincipal(invalidStudentId);

        // Act
        var result = await guard.ResolveAuthenticatedStudentAsync(principal);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
        result.StudentId.Should().BeNull();
    }

    [Fact]
    public async Task Given_DuplicateOrEmptyAlongsideValidStudentIdClaims_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var duplicatePrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, studentId.ToString()),
                    new Claim(StudentIdClaimType, studentId.ToString())
                ],
                authenticationType: "TestScheme"));
        var emptyAlongsideValidPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, string.Empty),
                    new Claim(StudentIdClaimType, studentId.ToString())
                ],
                authenticationType: "TestScheme"));

        // Act
        var duplicateResult = await guard.ResolveAuthenticatedStudentAsync(duplicatePrincipal);
        var emptyAlongsideValidResult = await guard.ResolveAuthenticatedStudentAsync(emptyAlongsideValidPrincipal);

        // Assert
        duplicateResult.Status.Should().Be(StudentAccessStatus.Forbidden);
        emptyAlongsideValidResult.Status.Should().Be(StudentAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_ContradictoryStudentIdOrAccountSubClaims_When_Resolving_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var contradictoryStudentIds = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, studentId.ToString()),
                    new Claim(StudentIdClaimType, Guid.NewGuid().ToString())
                ],
                authenticationType: "TestScheme"));
        var contradictorySubAndNameId = CreateStudentPrincipal(
            studentId.ToString(),
            sub: "account-1",
            nameIdentifier: "account-2");

        // Act
        var studentIdResult = await guard.ResolveAuthenticatedStudentAsync(contradictoryStudentIds);
        var accountResult = await guard.ResolveAuthenticatedStudentAsync(contradictorySubAndNameId);

        // Assert
        studentIdResult.Status.Should().Be(StudentAccessStatus.Forbidden);
        accountResult.Status.Should().Be(StudentAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_RoleInOneIdentityAndStudentIdInAnother_When_Authorizing_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var roleIdentity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Role, "Student")],
            authenticationType: "SchemeA");
        var idIdentity = new ClaimsIdentity(
            [new Claim(StudentIdClaimType, studentId.ToString())],
            authenticationType: "SchemeB");
        var principal = new ClaimsPrincipal([roleIdentity, idIdentity]);

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_TwoEligibleAuthenticatedStudentIdentities_When_Authorizing_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var firstIdentity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Student"),
                new Claim(StudentIdClaimType, studentId.ToString())
            ],
            authenticationType: "SchemeA");
        var secondIdentity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Role, "Student"),
                new Claim(StudentIdClaimType, Guid.NewGuid().ToString())
            ],
            authenticationType: "SchemeB");
        var principal = new ClaimsPrincipal([firstIdentity, secondIdentity]);

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
    }

    [Theory]
    [InlineData(SubjectClaimType)]
    [InlineData(ClaimTypes.NameIdentifier)]
    [InlineData(StudentIdClaimType)]
    [InlineData(RecruiterIdClaimType)]
    public async Task Given_AdditionalAuthenticatedIdentityWithIdentifier_When_Authorizing_Then_ReturnsForbidden(
        string claimType)
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = CreateStudentPrincipal(studentId.ToString());
        principal.AddIdentity(new ClaimsIdentity(
            [new Claim(claimType, Guid.NewGuid().ToString())],
            authenticationType: "OtherScheme"));

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_StudentIdentityWithContradictoryRecruiterId_When_Authorizing_Then_ReturnsForbidden()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.Role, "Student"),
                    new Claim(StudentIdClaimType, studentId.ToString()),
                    new Claim(RecruiterIdClaimType, Guid.NewGuid().ToString())
                ],
                authenticationType: "TestScheme"));

        // Act
        var result = await guard.AuthorizeStudentAccessAsync(principal, studentId);

        // Assert
        result.Status.Should().Be(StudentAccessStatus.Forbidden);
    }

    [Fact]
    public async Task Given_CanceledToken_When_Authorizing_Then_ThrowsOperationCanceledException()
    {
        // Arrange
        var studentId = Guid.NewGuid();
        var guard = new ClaimsPrincipalStudentAccessGuard();
        var principal = CreateStudentPrincipal(studentId.ToString());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        // Act
        Func<Task> act = async () =>
            await guard.AuthorizeStudentAccessAsync(principal, studentId, cancellation.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    private static ClaimsPrincipal CreateStudentPrincipal(
        string? studentId,
        string? sub = null,
        string? nameIdentifier = null)
    {
        var claims = new List<Claim> { new(ClaimTypes.Role, "Student") };

        if (studentId is not null)
        {
            claims.Add(new Claim(StudentIdClaimType, studentId));
        }

        if (sub is not null)
        {
            claims.Add(new Claim(SubjectClaimType, sub));
        }

        if (nameIdentifier is not null)
        {
            claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdentifier));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "TestScheme"));
    }
}
