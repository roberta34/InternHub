using InternHub.Domain.Students;
using Xunit;
using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Tests.Unit.Students;

public class StudentTests
{
    [Fact]
    public void Constructor_ValidData_CreatesStudent()
    {
        var student = new Student(
            Guid.NewGuid(),
            "  Ana Pop ",
            "ANA@mail.com");

        Assert.Equal("Ana Pop", student.FullName);
        Assert.Equal("ana@mail.com", student.Email);
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("Ana", "")]
    [InlineData("Ana", "not-an-email")]
    public void Constructor_InvalidData_Throws(string name, string email)
    {
        Assert.Throws<ArgumentException>(() =>
            new Student(
                Guid.NewGuid(),
                name,
                email));
    }

    [Fact]
    public void Constructor_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new Student(
                Guid.Empty,
                "Ana",
                "a@b.com"));
    }

    [Fact]
    public void Update_ValidData_ChangesStudentDetails()
    {
        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        student.Update(
            "Ion Pop",
            "ion@mail.com");

        Assert.Equal("Ion Pop", student.FullName);
        Assert.Equal("ion@mail.com", student.Email);
    }

    [Fact]
    public void Update_InvalidData_DoesNotPartiallyChangeStudent()
    {
        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        Assert.Throws<ArgumentException>(() =>
            student.Update(
                "",
                "updated@mail.com"));

        Assert.Equal("Ana Pop", student.FullName);
        Assert.Equal("ana@mail.com", student.Email);
    }
    
    [Fact]
    public void InternshipCandidacies_InitiallyIsEmpty()
    {
        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        Assert.Empty(student.InternshipCandidacies);
    }

    [Fact]
    public void InternshipCandidacies_IsReadOnly()
    {
        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        var collection =
            Assert.IsAssignableFrom<ICollection<InternshipCandidacy>>(
                student.InternshipCandidacies);

        Assert.True(collection.IsReadOnly);
    }
    
    [Fact]
    public void Update_WithInvalidEmail_DoesNotPartiallyChangeStudent()
    {
        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        Assert.Throws<ArgumentException>(() =>
            student.Update(
                "Ana Popescu",
                "invalid-email"));

        Assert.Equal("Ana Pop", student.FullName);
        Assert.Equal("ana@mail.com", student.Email);
    }
}