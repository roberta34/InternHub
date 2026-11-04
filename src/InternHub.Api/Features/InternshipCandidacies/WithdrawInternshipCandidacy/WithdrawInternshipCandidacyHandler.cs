using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public sealed class WithdrawInternshipCandidacyHandler(
    IInternshipCandidacyRepository candidacies) : IWithdrawInternshipCandidacyHandler
{
    public Result<bool> Handle(WithdrawInternshipCandidacyCommand command)
    {
        var candidacy = candidacies.GetById(command.CandidacyId);

        if (candidacy is null)
            return Result<bool>.NotFound(
                $"Candidacy {command.CandidacyId} not found.");

        if (!candidacy.Withdraw())
            return Result<bool>.Conflict(
                $"Candidacy {command.CandidacyId} cannot be withdrawn from status {candidacy.Status}.");

        return candidacies.Update(candidacy)
            ? Result<bool>.Success(true)
            : Result<bool>.NotFound(
                $"Candidacy {command.CandidacyId} not found.");
    }
}
