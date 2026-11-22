using InternHub.Api.Features.Notifications.GetRecruiterNotifications;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.Notifications;

public sealed class NotificationsFeatureModule : IFeatureModule
{
    public void RegisterServices(IServiceCollection services)
    {
        services.AddScoped<IGetRecruiterNotificationsHandler, GetRecruiterNotificationsHandler>();
    }

    public void MapEndpoints(RouteGroupBuilder group)
    {
        GetRecruiterNotifications.Endpoint.Map(group);
    }
}
