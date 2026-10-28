using FluentAssertions;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students.DeleteStudent;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.Students.Fakes;

namespace InternHub.Tests.Unit.Students;

public sealed class DeleteStudentHandlerTests
{
    [Fact]
    public void Given_ExistingStudent_When_DeletingStudent_Then_ReturnsSuccess()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new DeleteStudentHandler(repository);

        // Act
        var result = handler.Handle(new DeleteStudentCommand(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeTrue();
    }

    [Fact]
    public void Given_MissingStudent_When_DeletingStudent_Then_ReturnsNotFound()
    {
        // Arrange
        var repository = new FakeStudentRepository();
        var handler = new DeleteStudentHandler(repository);
        var missingId = Guid.NewGuid();

        // Act
        var result = handler.Handle(new DeleteStudentCommand(missingId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Be($"Student {missingId} not found.");
    }

    [Fact]
    public void Given_ExistingStudent_When_DeletingStudent_Then_RemovesStudentFromRepository()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new DeleteStudentHandler(repository);

        // Act
        handler.Handle(new DeleteStudentCommand(student.Id));

        // Assert
        repository.GetById(student.Id).Should().BeNull();
    }
}
