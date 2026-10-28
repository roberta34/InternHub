using FluentAssertions;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students.CreateStudent;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.Students;

public sealed class CreateStudentHandlerTests
{
    [Fact]
    public void Given_ValidCommand_When_CreatingStudent_Then_ReturnsSuccessAndPersistsStudent()
    {
        // Arrange
        var repository = new FakeStudentRepository();
        var handler = new CreateStudentHandler(repository);
        var command = new CreateStudentCommand("  Ana Pop  ", " ANA@MAIL.COM ");

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value!.Id.Should().NotBe(Guid.Empty);
        result.Value.FullName.Should().Be("Ana Pop");
        result.Value.Email.Should().Be("ana@mail.com");
        repository.GetById(result.Value.Id).Should().NotBeNull();
    }

    [Fact]
    public void Given_InvalidCommand_When_CreatingStudent_Then_ReturnsValidationWithoutPersistingStudent()
    {
        // Arrange
        var repository = new FakeStudentRepository();
        var handler = new CreateStudentHandler(repository);
        var command = new CreateStudentCommand(null, "ana@mail.com");

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("Full name is required.");
        result.Value.Should().BeNull();
        repository.GetAll().Should().BeEmpty();
    }

    [Fact]
    public void Given_RepositoryConflict_When_CreatingStudent_Then_ReturnsConflictWithoutValue()
    {
        // Arrange
        var repository = new FakeStudentRepository { AddResult = false };
        var handler = new CreateStudentHandler(repository);
        var command = new CreateStudentCommand("Ana Pop", "ana@mail.com");

        // Act
        var result = handler.Handle(command);

        // Assert
        result.Status.Should().Be(ResultStatus.Conflict);
        result.Error.Should().Contain("already exists");
        result.Value.Should().BeNull();
        repository.GetAll().Should().BeEmpty();
    }
}
