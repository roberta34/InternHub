using System.Security.Claims;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies/{id:guid}/withdraw", async (
            Guid id,
            ClaimsPrincipal user,
            IWithdrawInternshipCandidacyHandler handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.HandleAsync(
                new WithdrawInternshipCandidacyCommand(id, user),
                cancellationToken);

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
