namespace InternHub.Api.Features.Auth.RegisterStudent;

public sealed record RegisterStudentRequest(string? FullName, string? Email, string? Password);
