using InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;
using InternHub.Api.Features.InternshipCandidacies.StudentAccess;
using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternHub.Api.Features.InternshipCandidacies;

public sealed class CandidacySummaryModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.TryAddScoped<IStudentAccessGuard, ClaimsPrincipalStudentAccessGuard>();
        services.AddScoped<IGetStudentCandidacySummaryHandler, GetStudentCandidacySummaryHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        GetStudentCandidacySummary.Endpoint.Map(group);
    }
}
