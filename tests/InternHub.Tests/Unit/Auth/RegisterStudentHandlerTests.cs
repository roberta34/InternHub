using FluentAssertions;
using InternHub.Api.Domain.Auth;
using InternHub.Api.Features.Auth.RegisterStudent;
using InternHub.Api.Features.Shared;
using InternHub.Tests.Unit.Auth.Fakes;

namespace InternHub.Tests.Unit.Auth;

public sealed class RegisterStudentHandlerTests
{
    [Fact]
    public async Task Given_ValidRegistration_When_Handling_Then_ReturnsStudentAccount()
    {
        // Arrange
        var registeredAccount = new RegisteredAccount(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "student@example.com");
        var registration = new FakeStudentAccountRegistration
        {
            ResultToReturn = Result<RegisteredAccount>.Success(registeredAccount)
        };
        var handler = new RegisterStudentHandler(registration);
        var command = new RegisterStudentCommand(
            "Student Name",
            "student@example.com",
            "test-password");

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(new RegisterStudentResponse(
            registeredAccount.AccountId,
            registeredAccount.StudentId,
            registeredAccount.Email,
            "Student"));
        registration.CallCount.Should().Be(1);
        registration.LastFullName.Should().Be(command.FullName);
        registration.LastEmail.Should().Be(command.Email);
        registration.LastPassword.Should().Be(command.Password);
    }

    [Theory]
    [InlineData(null, "student@example.com", "test-password")]
    [InlineData("", "student@example.com", "test-password")]
    [InlineData(" ", "student@example.com", "test-password")]
    [InlineData("Student Name", null, "test-password")]
    [InlineData("Student Name", "", "test-password")]
    [InlineData("Student Name", " ", "test-password")]
    [InlineData("Student Name", "student@example.com", null)]
    [InlineData("Student Name", "student@example.com", "")]
    [InlineData("Student Name", "student@example.com", " ")]
    public async Task Given_EmptyRegistrationField_When_Handling_Then_ReturnsValidationWithoutCallingPort(
        string? fullName,
        string? email,
        string? password)
    {
        // Arrange
        var registration = new FakeStudentAccountRegistration();
        var handler = new RegisterStudentHandler(registration);
        var command = new RegisterStudentCommand(fullName, email, password);

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Value.Should().BeNull();
        registration.CallCount.Should().Be(0);
    }

    [Fact]
    public async Task Given_DomainArgumentException_When_Registering_Then_ReturnsValidation()
    {
        // Arrange
        var registration = new FakeStudentAccountRegistration
        {
            ExceptionToThrow = new ArgumentException("Full name is required.", "fullName")
        };
        var handler = new RegisterStudentHandler(registration);
        var command = new RegisterStudentCommand(
            "Student Name",
            "student@example.com",
            "test-password");

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("Full name is required.");
        result.Value.Should().BeNull();
        registration.CallCount.Should().Be(1);
    }

    [Fact]
    public async Task Given_EmailAlreadyExists_When_Registering_Then_ReturnsConflict()
    {
        // Arrange
        var registration = new FakeStudentAccountRegistration
        {
            ResultToReturn = Result<RegisteredAccount>.Conflict(
                "An account with this email already exists.")
        };
        var handler = new RegisterStudentHandler(registration);
        var command = new RegisterStudentCommand(
            "Student Name",
            "student@example.com",
            "test-password");

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
        result.Value.Should().BeNull();
        registration.CallCount.Should().Be(1);
    }

    [Theory]
    [MemberData(nameof(PortFailureResults))]
    public async Task Given_PortFailure_When_Registering_Then_ForwardsFailureWithoutValue(
        Result<RegisteredAccount> portResult,
        ResultStatus expectedStatus,
        string expectedError)
    {
        // Arrange
        var registration = new FakeStudentAccountRegistration
        {
            ResultToReturn = portResult
        };
        var handler = new RegisterStudentHandler(registration);
        var command = new RegisterStudentCommand(
            "Student Name",
            "student@example.com",
            "test-password");

        // Act
        var result = await handler.HandleAsync(command, CancellationToken.None);

        // Assert
        result.Status.Should().Be(expectedStatus);
        result.Error.Should().Be(expectedError);
        result.Value.Should().BeNull();
        registration.CallCount.Should().Be(1);
    }

    public static TheoryData<Result<RegisteredAccount>, ResultStatus, string> PortFailureResults =>
        new()
        {
            {
                Result<RegisteredAccount>.Validation("Registration validation failed."),
                ResultStatus.Validation,
                "Registration validation failed."
            },
            {
                Result<RegisteredAccount>.NotFound("Registration target not found."),
                ResultStatus.NotFound,
                "Registration target not found."
            },
            {
                Result<RegisteredAccount>.Unauthorized("Registration is unauthorized."),
                ResultStatus.Unauthorized,
                "Registration is unauthorized."
            },
            {
                Result<RegisteredAccount>.ServiceUnavailable("Registration is unavailable."),
                ResultStatus.ServiceUnavailable,
                "Registration is unavailable."
            }
        };
}
