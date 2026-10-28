using InternHub.Api.Features.Offers.CreateOffer;
using InternHub.Api.Features.Offers.DeleteOffer;
using InternHub.Api.Features.Offers.GetOfferById;
using InternHub.Api.Features.Offers.GetOffers;
using InternHub.Api.Features.Offers.UpdateOffer;

namespace InternHub.Api.Features.Offers;

public static class OffersFeaturesExtensions
{
    public static IServiceCollection AddOfferFeatures(this IServiceCollection services)
    {
        services.AddScoped<ICreateOfferHandler, CreateOfferHandler>();
        services.AddScoped<IGetOfferByIdHandler, GetOfferByIdHandler>();
        services.AddScoped<IGetOffersHandler, GetOffersHandler>();
        services.AddScoped<IUpdateOfferHandler, UpdateOfferHandler>();
        services.AddScoped<IDeleteOfferHandler, DeleteOfferHandler>();

        return services;
    }
}
