using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public interface ICreateInternshipCandidacyHandler
{
    Task<Result<CreateInternshipCandidacyResponse>> HandleAsync(
        CreateInternshipCandidacyCommand command,
        CancellationToken cancellationToken);
}
