using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Offers;

namespace InternHub.Api.Features.Offers.UpdateOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPut("/offers/{id:guid}", (
            Guid id,
            UpdateOfferRequest request,
            IUpdateOfferHandler handler,
            HttpContext httpContext) =>
        {
            if (!RecruiterAuthorization.TryGetRecruiterId(httpContext, out var recruiterId))
                return Results.Json("Recruiter authentication is required.", statusCode: StatusCodes.Status401Unauthorized);

            var result = handler.Handle(new UpdateOfferCommand(
                id,
                request.Title,
                request.CompanyName,
                request.Requirements,
                request.Deadline,
                request.MaximumAvailableSlots,
                request.Version,
                recruiterId));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
