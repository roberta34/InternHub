namespace InternHub.Api.Features.Shared;

public static class ApiRoutes
{
    public static RouteGroupBuilder MapV1(this WebApplication app) =>
        app.MapGroup("/api/v1");
}
