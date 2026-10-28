using InternHub.Domain.Students;

namespace InternHub.Tests.Unit.Students.Fakes;

internal sealed class FakeStudentRepository : IStudentRepository
{
    private readonly Dictionary<Guid, Student> _students = new();

    public bool Add(Student student) => _students.TryAdd(student.Id, student);

    public Student? GetById(Guid id) =>
        _students.TryGetValue(id, out var student) ? student : null;

    public IReadOnlyList<Student> GetAll() => _students.Values.ToList();

    public bool Update(Student student)
    {
        if (!_students.ContainsKey(student.Id))
            return false;

        _students[student.Id] = student;
        return true;
    }

    public bool Delete(Guid id) => _students.Remove(id);
}
