using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public interface IDecideInternshipCandidacyHandler
{
    Result<DecideInternshipCandidacyResponse> Handle(DecideInternshipCandidacyCommand command);
}
