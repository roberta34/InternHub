using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Features.Shared;

public interface IFeatureModule
{
    void RegisterServices(IServiceCollection services);

    void MapEndpoints(RouteGroupBuilder group);
}