using FluentAssertions;
using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Students.GetStudents;
using InternHub.Domain.Students;
using InternHub.Tests.Unit.Students.Fakes;
using InternHub.Features.Students.GetStudents;

namespace InternHub.Tests.Unit.Students;

public sealed class GetStudentsHandlerTests
{
    [Fact]
    public void Given_EmptyRepository_When_GettingStudents_Then_ReturnsEmptySuccessList()
    {
        // Arrange
        var repository = new FakeStudentRepository();
        var handler = new GetStudentsHandler(repository);

        // Act
        var result = handler.Handle(new GetStudentsQuery());

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().NotBeNull();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public void Given_RepositoryStudents_When_GettingStudents_Then_ReturnsAllStudentResponses()
    {
        // Arrange
        var firstStudent = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var secondStudent = new Student(Guid.NewGuid(), "Ion Pop", "ion@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(firstStudent);
        repository.Add(secondStudent);
        var handler = new GetStudentsHandler(repository);

        // Act
        var result = handler.Handle(new GetStudentsQuery());

        // Assert
        result.Status.Should().Be(ResultStatus.Success);
        result.Value.Should().BeEquivalentTo(
            new[]
            {
                new GetStudentsResponse(firstStudent.Id, "Ana Pop", "ana@mail.com"),
                new GetStudentsResponse(secondStudent.Id, "Ion Pop", "ion@mail.com")
            },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public void Given_RepositoryStudents_When_GettingStudents_Then_DoesNotMutateRepository()
    {
        // Arrange
        var firstStudent = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");
        var secondStudent = new Student(Guid.NewGuid(), "Ion Pop", "ion@mail.com");
        var repository = new FakeStudentRepository();
        repository.Add(firstStudent);
        repository.Add(secondStudent);
        var handler = new GetStudentsHandler(repository);

        // Act
        handler.Handle(new GetStudentsQuery());

        // Assert
        repository.GetAll().Should().HaveCount(2);
        repository.GetById(firstStudent.Id).Should().BeSameAs(firstStudent);
        repository.GetById(secondStudent.Id).Should().BeSameAs(secondStudent);
    }
}
