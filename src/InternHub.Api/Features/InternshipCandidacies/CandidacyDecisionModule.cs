using InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;
using InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;
using InternHub.Api.Features.InternshipCandidacies.RecruiterAccess;
using InternHub.Api.Features.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InternHub.Api.Features.InternshipCandidacies;

public sealed class CandidacyDecisionModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.TryAddScoped<IRecruiterAccessGuard, ClaimsPrincipalRecruiterAccessGuard>();
        services.AddScoped<IDecideInternshipCandidacyHandler, DecideInternshipCandidacyHandler>();
        services.AddScoped<IGetOfferCandidaciesHandler, GetOfferCandidaciesHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        DecideInternshipCandidacy.Endpoint.Map(group);
        GetOfferCandidacies.Endpoint.Map(group);
    }
}
