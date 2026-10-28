using FluentAssertions;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students.GetStudentById;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.Students.Fakes;
using InternHub.Features.Students.GetStudentById;

namespace InternHub.Tests.Unit.Students;

public sealed class GetStudentByIdHandlerTests
{
    [Fact]
    public void Given_ExistingStudentId_When_GettingStudentById_Then_ReturnsStudentResponse()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new GetStudentByIdHandler(repository);

        // Act
        var result = handler.Handle(new GetStudentByIdQuery(student.Id));

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(
            new GetStudentByIdResponse(student.Id, "Ana Pop", "ana@mail.com"));
    }

    [Fact]
    public void Given_UnknownStudentId_When_GettingStudentById_Then_ReturnsNotFound()
    {
        // Arrange
        var repository = new FakeStudentRepository();
        var handler = new GetStudentByIdHandler(repository);
        var studentId = Guid.NewGuid();

        // Act
        var result = handler.Handle(new GetStudentByIdQuery(studentId));

        // Assert
        result.Status.Should().Be(ResultStatus.NotFound);
        result.Error.Should().Contain(studentId.ToString());
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Given_ExistingStudent_When_GettingStudentById_Then_DoesNotMutateRepository()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(student);
        var handler = new GetStudentByIdHandler(repository);

        // Act
        handler.Handle(new GetStudentByIdQuery(student.Id));

        // Assert
        repository.GetAll().Should().ContainSingle()
            .Which.Should().BeSameAs(student);
    }
}
