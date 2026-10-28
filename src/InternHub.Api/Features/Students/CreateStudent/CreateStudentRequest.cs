namespace InternHub.Api.Features.Students.CreateStudent;

public sealed record CreateStudentRequest(
    string? FullName,
    string? Email);