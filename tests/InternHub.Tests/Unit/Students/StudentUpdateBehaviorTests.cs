using FluentAssertions;
using InternHub.Domain.Students;

namespace InternHub.Tests.Unit.Students;

public sealed class StudentUpdateBehaviorTests
{
    [Fact]
    public void Given_UnnormalizedStudentData_When_UpdatingStudent_Then_NormalizesNameAndEmail()
    {
        // Arrange
        var student = new Student(Guid.NewGuid(), "Ana Pop", "ana@mail.com");

        // Act
        student.Update("  Ion Pop  ", " ION@MAIL.COM ");

        // Assert
        student.FullName.Should().Be("Ion Pop");
        student.Email.Should().Be("ion@mail.com");
    }
}
