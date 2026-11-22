using InternHub.Api.Features.Shared;
using InternHub.Api.Features.Offers;

namespace InternHub.Api.Features.Offers.CloseOffer;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/offers/{id:guid}/close", (
            Guid id,
            ICloseOfferHandler handler,
            HttpContext httpContext) =>
        {
            if (!RecruiterAuthorization.TryGetRecruiterId(httpContext, out var recruiterId))
                return Results.Json("Recruiter authentication is required.", statusCode: StatusCodes.Status401Unauthorized);

            var result = handler.Handle(new CloseOfferCommand(id, recruiterId));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}