namespace InternHub.Api.Features.InternshipCandidacies.StudentAccess;

public enum StudentAccessStatus
{
    Authorized,
    Unauthorized,
    Forbidden
}

public sealed class StudentAccessResult
{
    private StudentAccessResult(
        StudentAccessStatus status,
        Guid? studentId,
        string? error)
    {
        Status = status;
        StudentId = studentId;
        Error = error;
    }

    public StudentAccessStatus Status { get; }
    public Guid? StudentId { get; }
    public string? Error { get; }

    public static StudentAccessResult Authorized(Guid studentId)
    {
        if (studentId == Guid.Empty)
        {
            throw new ArgumentException("StudentId must not be empty.", nameof(studentId));
        }

        return new StudentAccessResult(StudentAccessStatus.Authorized, studentId, null);
    }

    public static StudentAccessResult Unauthorized(string error) =>
        new(StudentAccessStatus.Unauthorized, null, error);

    public static StudentAccessResult Forbidden(string error) =>
        new(StudentAccessStatus.Forbidden, null, error);
}
