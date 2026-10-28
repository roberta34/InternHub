using FluentAssertions;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students.UpdateStudent;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.Students;

public sealed class UpdateStudentHandlerTests
{
    [Fact]
    public void Given_ExistingStudentWithValidData_When_UpdatingStudent_Then_ReturnsSuccess()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new UpdateStudentHandler(repository);

        // Act
        var result = handler.Handle(
            new UpdateStudentCommand(student.Id, "Ana Popescu", "ana.popescu@mail.com"));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void Given_ExistingStudentWithInvalidData_When_UpdatingStudent_Then_ReturnsValidation()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new UpdateStudentHandler(repository);

        // Act
        var result = handler.Handle(
            new UpdateStudentCommand(student.Id, "", "updated@mail.com"));

        // Assert
        result.Status.Should().Be(ResultStatus.Validation);
        result.Error.Should().Contain("Full name is required.");
    }

    [Fact]
    public void Given_MissingStudent_When_UpdatingStudent_Then_ReturnsNotFound()
    {
        // Arrange
        var repository = new FakeStudentRepository();
        var handler = new UpdateStudentHandler(repository);
        var missingId = Guid.NewGuid();

        // Act
        var result = handler.Handle(
            new UpdateStudentCommand(missingId, "Ana Pop", "ana@mail.com"));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Student {missingId} not found.");
    }

    [Fact]
    public void Given_ExistingStudent_When_UpdatingWithValidData_Then_PersistsExpectedChanges()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new UpdateStudentHandler(repository);

        // Act
        handler.Handle(
            new UpdateStudentCommand(student.Id, "Ana Popescu", "updated@mail.com"));

        // Assert
        var updatedStudent = repository.GetById(student.Id);
        updatedStudent.Should().NotBeNull();
        updatedStudent!.FullName.Should().Be("Ana Popescu");
        updatedStudent.Email.Should().Be("updated@mail.com");
    }
}
