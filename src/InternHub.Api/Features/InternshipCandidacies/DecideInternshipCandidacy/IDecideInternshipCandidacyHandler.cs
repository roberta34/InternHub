using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public interface IDecideInternshipCandidacyHandler
{
    Task<Result<DecideInternshipCandidacyResponse>> HandleAsync(
        DecideInternshipCandidacyCommand command,
        CancellationToken cancellationToken = default);
}
