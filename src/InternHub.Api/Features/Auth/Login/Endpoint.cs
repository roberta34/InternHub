using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Identity;

namespace InternHub.Api.Features.Auth.Login;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/auth/login", async (
            LoginRequest request,
            ILoginHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(
                new LoginCommand(request.Email, request.Password),
                ct);

            return result.Status == ResultStatus.Success
                ? Results.SignIn(
                    result.Value!,
                    authenticationScheme: IdentityConstants.BearerScheme)
                : result.ToFailure();
        });
}
