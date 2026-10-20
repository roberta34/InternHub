using System.Collections.Concurrent;
using InternHub.Domain.Students;

namespace InternHub.Infrastructure.Repositories;

public sealed class InMemoryStudentRepository : IStudentRepository
{
    private readonly ConcurrentDictionary<Guid, Student> _store = new();

    public bool Add(Student student) => _store.TryAdd(student.Id, student);

    public Student? GetById(Guid id) => _store.TryGetValue(id, out var s) ? s : null;

    public IReadOnlyList<Student> GetAll() => _store.Values.ToList();

    public bool Update(Student student)
    {
        if (!_store.TryGetValue(student.Id, out var current)) return false;
        return _store.TryUpdate(student.Id, student, current);
    }

    public bool Delete(Guid id) => _store.TryRemove(id, out _);
}