namespace InternHub.Api.Features.Auth.GetCurrentUser;

public sealed record GetCurrentUserQuery(
    string? Subject,
    string? Role,
    string? StudentId,
    string? RecruiterId);
