namespace InternHub.Api.Features.Auth.RegisterStudent;

public sealed record RegisterStudentCommand(string? FullName, string? Email, string? Password);
