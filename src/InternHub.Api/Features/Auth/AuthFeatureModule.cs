using InternHub.Api.Features.Auth.GetCurrentUser;
using InternHub.Api.Features.Auth.Login;
using InternHub.Api.Features.Auth.RegisterStudent;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Auth;

public sealed class AuthFeatureModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IRegisterStudentHandler, RegisterStudentHandler>();
        services.AddScoped<ILoginHandler, LoginHandler>();
        services.AddScoped<IGetCurrentUserHandler, GetCurrentUserHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        RegisterStudent.Endpoint.Map(group);
        Login.Endpoint.Map(group);
        GetCurrentUser.Endpoint.Map(group);
    }
}
