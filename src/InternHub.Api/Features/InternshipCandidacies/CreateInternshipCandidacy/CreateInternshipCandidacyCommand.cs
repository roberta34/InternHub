namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public sealed record CreateInternshipCandidacyCommand(Guid StudentId, Guid OfferId);
