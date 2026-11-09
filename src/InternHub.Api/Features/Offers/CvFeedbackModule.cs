using InternHub.Api.Features.Offers.CvFeedback;
using InternHub.Api.Features.Shared;
using InternHub.Api.Infrastructure.AI;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Features.Offers;

public sealed class CvFeedbackModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICvFeedbackHandler, CvFeedbackHandler>();
        services.AddRecruiterEvaluation();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        CvFeedback.Endpoint.Map(group);
    }
}
