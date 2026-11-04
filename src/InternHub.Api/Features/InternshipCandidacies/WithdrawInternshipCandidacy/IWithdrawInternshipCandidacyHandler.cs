using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public interface IWithdrawInternshipCandidacyHandler
{
    Result<bool> Handle(WithdrawInternshipCandidacyCommand command);
}
