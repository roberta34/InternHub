using InternHub.Domain.Students;
using InternHub.Infrastructure.Repositories;
using Xunit;

namespace InternHub.Tests.Unit.Students;

public class StudentTests
{
    [Fact]
    public void Constructor_ValidData_CreatesStudent()
    {
        var s = new Student(Guid.NewGuid(), "  Ana Pop ", "ANA@mail.com");
        Assert.Equal("Ana Pop", s.FullName);
        Assert.Equal("ana@mail.com", s.Email);
    }

    [Theory]
    [InlineData("", "a@b.com")]
    [InlineData("Ana", "")]
    [InlineData("Ana", "not-an-email")]
    public void Constructor_InvalidData_Throws(string name, string email) =>
        Assert.Throws<ArgumentException>(() => new Student(Guid.NewGuid(), name, email));

    [Fact]
    public void Constructor_EmptyId_Throws() =>
        Assert.Throws<ArgumentException>(() => new Student(Guid.Empty, "Ana", "a@b.com"));
}

public class InMemoryStudentRepositoryTests
{
    [Fact]
    public void Add_DuplicateId_ReturnsFalse()
    {
        var repo = new InMemoryStudentRepository();
        var id = Guid.NewGuid();
        Assert.True(repo.Add(new Student(id, "Ana", "a@b.com")));
        Assert.False(repo.Add(new Student(id, "Alt Nume", "x@y.com")));
    }

    [Fact]
    public void GetById_Existing_ReturnsStudent()
    {
        var repo = new InMemoryStudentRepository();
        var s = new Student(Guid.NewGuid(), "Ana", "a@b.com");
        repo.Add(s);
        Assert.Same(s, repo.GetById(s.Id));
    }

    [Fact]
    public void GetById_Missing_ReturnsNull() =>
        Assert.Null(new InMemoryStudentRepository().GetById(Guid.NewGuid()));

    [Fact]
    public void GetAll_ReturnsSnapshot()
    {
        var repo = new InMemoryStudentRepository();
        repo.Add(new Student(Guid.NewGuid(), "Ana", "a@b.com"));
        var snapshot = repo.GetAll();
        repo.Add(new Student(Guid.NewGuid(), "Ion", "i@b.com"));
        Assert.Single(snapshot);
        Assert.Equal(2, repo.GetAll().Count);
    }
}