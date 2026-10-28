namespace InternHub.Api.Features.Students.UpdateStudent;

public sealed record UpdateStudentCommand(Guid Id, string? FullName, string? Email);
