using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Offers.GetOfferById;
using InternHub.Api.Features.Offers.GetOffers;

namespace InternHub.Api.Features.Offers;

public static class OffersFeaturesExtensions
{
    public static IServiceCollection AddOfferFeatures(this IServiceCollection services)
    {
        services.AddScoped<ICreateOfferHandler, CreateOfferHandler>();
        services.AddScoped<IGetOfferByIdHandler, GetOfferByIdHandler>();
        services.AddScoped<IGetOffersHandler, GetOffersHandler>();

        return services;
    }
}
