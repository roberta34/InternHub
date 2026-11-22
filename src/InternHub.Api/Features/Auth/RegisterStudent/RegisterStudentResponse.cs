namespace InternHub.Api.Features.Auth.RegisterStudent;

public sealed record RegisterStudentResponse(
    Guid AccountId,
    Guid StudentId,
    string Email,
    string Role);
