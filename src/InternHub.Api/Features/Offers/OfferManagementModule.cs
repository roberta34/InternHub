using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Offers.CloseOffer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace InternHub.Api.Features.Offers;

public sealed class OfferManagementModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<ICloseOfferHandler, CloseOfferHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        CloseOffer.Endpoint.Map(group);
    }
}