using InternHub.Domain.Students;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;

namespace InternHub.Api.Infrastructure.Persistence;

public sealed class EfCoreStudentRepository(InternHubDbContext db) : IStudentRepository
{
    public bool Add(Student student)
    {
        db.Students.Add(student);
        try
        {
            db.SaveChanges();
        }
        catch (DbUpdateException exception)
            when (IsDuplicateStudentEmailConflict(exception))
        {
            return false;
        }

        return true;
    }

    private static bool IsDuplicateStudentEmailConflict(DbUpdateException exception) =>
        exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19
        } sqliteException
        && sqliteException.Message.Contains(
            "Students.Email",
            StringComparison.OrdinalIgnoreCase);

    public Student? GetById(Guid id)
    {
        return db.Students
            .Include(s => s.InternshipCandidacies)
            .FirstOrDefault(s => s.Id == id);
    }

    public IReadOnlyList<Student> GetAll()
    {
        return db.Students
            .Include(s => s.InternshipCandidacies)
            .ToList();
    }

    public bool Update(Student student)
    {
        var exists = db.Students.Any(s => s.Id == student.Id);
        if (!exists)
            return false;

        db.Students.Update(student);
        db.SaveChanges();
        return true;
    }

    public bool Delete(Guid id)
    {
        var student = db.Students.FirstOrDefault(s => s.Id == id);
        if (student is null)
            return false;

        db.Students.Remove(student);
        db.SaveChanges();
        return true;
    }
}
