using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public static class Endpoint
{
    public static void Map(RouteGroupBuilder api) =>
        api.MapPost("/candidacies/{id:guid}/withdraw", (
            Guid id,
            IWithdrawInternshipCandidacyHandler handler) =>
        {
            var result = handler.Handle(new WithdrawInternshipCandidacyCommand(id));

            return result.Status == ResultStatus.Success
                ? Results.NoContent()
                : result.ToFailure();
        });
}
