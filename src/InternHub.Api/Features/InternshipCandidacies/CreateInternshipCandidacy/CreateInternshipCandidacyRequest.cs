namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public sealed record CreateInternshipCandidacyRequest(Guid StudentId, Guid OfferId);
