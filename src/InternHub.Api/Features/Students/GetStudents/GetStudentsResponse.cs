namespace InternHub.Features.Students.GetStudents;

public sealed record GetStudentsResponse(Guid Id, string FullName, string Email);