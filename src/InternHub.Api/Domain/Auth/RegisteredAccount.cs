namespace InternHub.Api.Domain.Auth;

public sealed record RegisteredAccount(Guid AccountId, Guid StudentId, string Email);
