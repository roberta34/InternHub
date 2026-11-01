using InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;
using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;
using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Features.InternshipCandidacies;

public sealed class CandidacyApplicationModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICreateInternshipCandidacyHandler, CreateInternshipCandidacyHandler>();
        services.AddScoped<IGetStudentCandidaciesHandler, GetStudentCandidaciesHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        CreateInternshipCandidacy.Endpoint.Map(group);
        GetStudentCandidacies.Endpoint.Map(group);
    }
}
