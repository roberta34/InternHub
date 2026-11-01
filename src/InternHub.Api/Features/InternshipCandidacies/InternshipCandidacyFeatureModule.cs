using InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;
using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Features.InternshipCandidacies;

public sealed class InternshipCandidacyFeatureModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IGetInternshipCandidacyByIdHandler, GetInternshipCandidacyByIdHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        GetInternshipCandidacyById.Endpoint.Map(group);
    }
}
