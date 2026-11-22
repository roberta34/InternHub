using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.Login;

public interface ILoginHandler
{
    Task<Result<ClaimsPrincipal>> HandleAsync(LoginCommand command, CancellationToken ct);
}
