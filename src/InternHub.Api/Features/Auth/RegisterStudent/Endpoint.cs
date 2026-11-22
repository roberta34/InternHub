using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth.RegisterStudent;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/auth/register-student", async (
            RegisterStudentRequest request,
            IRegisterStudentHandler handler,
            CancellationToken ct) =>
        {
            var result = await handler.HandleAsync(
                new RegisterStudentCommand(request.FullName, request.Email, request.Password),
                ct);

            return result.Status == ResultStatus.Success
                ? Results.Created("/api/v1/auth/me", result.Value)
                : result.ToFailure();
        });
}
