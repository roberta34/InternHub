namespace InternHub.Features.Students.CreateStudent;

public sealed record CreateStudentResponse(
    Guid Id,
    string FullName,
    string Email);