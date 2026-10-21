using InternHub.Domain.Students;
using InternHub.Infrastructure.Repositories;

namespace InternHub.Tests.Unit.Students;

public class InMemoryStudentRepositoryTests
{
    [Fact]
    public void Add_DuplicateId_ReturnsFalse()
    {
        var repository = new InMemoryStudentRepository();
        var id = Guid.NewGuid();

        Assert.True(
            repository.Add(
                new Student(
                    id,
                    "Ana",
                    "a@b.com")));

        Assert.False(
            repository.Add(
                new Student(
                    id,
                    "Alt Nume",
                    "x@y.com")));
    }

    [Fact]
    public void GetById_Existing_ReturnsStudent()
    {
        var repository = new InMemoryStudentRepository();

        var student = new Student(
            Guid.NewGuid(),
            "Ana",
            "a@b.com");

        repository.Add(student);

        var result = repository.GetById(student.Id);

        Assert.Same(student, result);
    }

    [Fact]
    public void GetById_Missing_ReturnsNull()
    {
        var repository = new InMemoryStudentRepository();

        var result =
            repository.GetById(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void GetAll_ReturnsSnapshot()
    {
        var repository = new InMemoryStudentRepository();

        repository.Add(
            new Student(
                Guid.NewGuid(),
                "Ana",
                "a@b.com"));

        var snapshot = repository.GetAll();

        repository.Add(
            new Student(
                Guid.NewGuid(),
                "Ion",
                "i@b.com"));

        Assert.Single(snapshot);
        Assert.Equal(2, repository.GetAll().Count);
    }
    
    [Fact]
    public void Add_NewStudent_ReturnsTrue()
    {
        var repository = new InMemoryStudentRepository();

        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        var result = repository.Add(student);

        Assert.True(result);
    }
    
    [Fact]
    public void GetAll_ReturnsAllStudents()
    {
        var repository = new InMemoryStudentRepository();

        var firstStudent = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        var secondStudent = new Student(
            Guid.NewGuid(),
            "Ion Popescu",
            "ion@mail.com");

        repository.Add(firstStudent);
        repository.Add(secondStudent);

        var result = repository.GetAll();

        Assert.Equal(2, result.Count);
        Assert.Contains(result, student => student.Id == firstStudent.Id);
        Assert.Contains(result, student => student.Id == secondStudent.Id);
    }
    
    [Fact]
    public void Update_ExistingStudent_ReturnsTrueAndUpdatesStudent()
    {
        var repository = new InMemoryStudentRepository();
        var id = Guid.NewGuid();

        var student = new Student(
            id,
            "Ana Pop",
            "ana@mail.com");

        repository.Add(student);

        var updatedStudent = new Student(
            id,
            "Ana Popescu",
            "ana.popescu@mail.com");

        var result = repository.Update(updatedStudent);

        Assert.True(result);

        var storedStudent = repository.GetById(id);

        Assert.NotNull(storedStudent);
        Assert.Equal("Ana Popescu", storedStudent.FullName);
        Assert.Equal("ana.popescu@mail.com", storedStudent.Email);
    }
    
    [Fact]
    public void Update_MissingStudent_ReturnsFalse()
    {
        var repository = new InMemoryStudentRepository();

        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        var result = repository.Update(student);

        Assert.False(result);
    }
    
    [Fact]
    public void Delete_ExistingStudent_ReturnsTrueAndRemovesStudent()
    {
        var repository = new InMemoryStudentRepository();

        var student = new Student(
            Guid.NewGuid(),
            "Ana Pop",
            "ana@mail.com");

        repository.Add(student);

        var result = repository.Delete(student.Id);

        Assert.True(result);
        Assert.Null(repository.GetById(student.Id));
    }
    
    [Fact]
    public void Delete_MissingStudent_ReturnsFalse()
    {
        var repository = new InMemoryStudentRepository();

        var result = repository.Delete(Guid.NewGuid());

        Assert.False(result);
    }
}