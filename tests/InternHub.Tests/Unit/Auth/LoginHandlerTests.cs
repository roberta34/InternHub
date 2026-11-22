using System.Security.Claims;
using FluentAssertions;
using InternHub.Api.Features.Auth.Login;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Auth.Fakes;

namespace InternHub.Tests.Unit.Auth;

public sealed class LoginHandlerTests
{
    [Fact]
    public async Task Given_ValidCredentials_When_Handling_Then_ReturnsAuthenticatedPrincipal()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", Guid.NewGuid().ToString("D"))],
            "test"));
        var authenticator = new FakeAccountAuthenticator
        {
            ResultToReturn = Result<ClaimsPrincipal>.Success(principal)
        };
        var handler = new LoginHandler(authenticator);
        var command = new LoginCommand("student@example.com", "test-password");

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeSameAs(principal);
        authenticator.CallCount.Should().Be(1);
        authenticator.LastEmail.Should().Be(command.Email);
        authenticator.LastPassword.Should().Be(command.Password);
    }

    [Fact]
    public async Task Given_WrongPasswordOrMissingAccount_When_Handling_Then_ReturnsSameUnauthorizedMessage()
    {
        // Arrange
        var wrongPasswordAuthenticator = new FakeAccountAuthenticator
        {
            ResultToReturn = Result<ClaimsPrincipal>.Unauthorized("Wrong password.")
        };
        var missingAccountAuthenticator = new FakeAccountAuthenticator
        {
            ResultToReturn = Result<ClaimsPrincipal>.Unauthorized("Account not found.")
        };
        var wrongPasswordHandler = new LoginHandler(wrongPasswordAuthenticator);
        var missingAccountHandler = new LoginHandler(missingAccountAuthenticator);
        var wrongPasswordCommand = new LoginCommand("student@example.com", "wrong-password");
        var missingAccountCommand = new LoginCommand("missing@example.com", "test-password");

        // Act
        var wrongPasswordResult = await wrongPasswordHandler.HandleAsync(
            wrongPasswordCommand,
            CancellationToken.None);
        var missingAccountResult = await missingAccountHandler.HandleAsync(
            missingAccountCommand,
            CancellationToken.None);

        // Assert
        wrongPasswordResult.Status.Should().Be(ResultStatus.Unauthorized);
        missingAccountResult.Status.Should().Be(ResultStatus.Unauthorized);
        wrongPasswordResult.Error.Should().Be("Invalid email or password.");
        missingAccountResult.Error.Should().Be(wrongPasswordResult.Error);
        wrongPasswordAuthenticator.CallCount.Should().Be(1);
        missingAccountAuthenticator.CallCount.Should().Be(1);
    }

    [Theory]
    [InlineData(null, "test-password")]
    [InlineData("", "test-password")]
    [InlineData(" ", "test-password")]
    [InlineData("student@example.com", null)]
    [InlineData("student@example.com", "")]
    [InlineData("student@example.com", " ")]
    public async Task Given_EmptyCredentialField_When_Handling_Then_ReturnsValidationWithoutCallingPort(
        string? email,
        string? password)
    {
        // Arrange
        var authenticator = new FakeAccountAuthenticator();
        var handler = new LoginHandler(authenticator);
        var command = new LoginCommand(email, password);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Value.Should().BeNull();
        authenticator.CallCount.Should().Be(0);
    }
}
