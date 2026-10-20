namespace InternHub.Domain.Students;

public interface IStudentRepository
{
    bool Add(Student student);
    Student? GetById(Guid id);
    IReadOnlyList<Student> GetAll();
    bool Update(Student student);
    bool Delete(Guid id);
}