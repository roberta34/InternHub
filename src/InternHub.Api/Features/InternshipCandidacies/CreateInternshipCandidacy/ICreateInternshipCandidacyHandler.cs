using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public interface ICreateInternshipCandidacyHandler
{
    Result<CreateInternshipCandidacyResponse> Handle(CreateInternshipCandidacyCommand command);
}
