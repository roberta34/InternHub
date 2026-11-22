using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Offers;

namespace InternHub.Api.Features.Offers.CreateOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/offers", (CreateOfferRequest request, ICreateOfferHandler handler, HttpContext httpContext) =>
        {
            if (!RecruiterAuthorization.TryGetRecruiterId(httpContext, out var recruiterId))
                return Results.Json("Recruiter authentication is required.", statusCode: StatusCodes.Status401Unauthorized);

            var result = handler.Handle(new CreateOfferCommand(
                request.Title,
                request.CompanyName,
                request.Requirements,
                request.Deadline,
                request.MaximumAvailableSlots,
                recruiterId));

            return result.Status == ResultStatus.Success
                ? Results.Created($"/api/v1/offers/{result.Value!.Id}", result.Value)
                : result.ToFailure();
        });
}
