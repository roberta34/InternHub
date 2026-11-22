using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public interface IWithdrawInternshipCandidacyHandler
{
    Task<Result<bool>> HandleAsync(
        WithdrawInternshipCandidacyCommand command,
        CancellationToken cancellationToken = default);
}
