namespace InternHub.Api.Features.Auth.Login;

public sealed record LoginCommand(string? Email, string? Password);
