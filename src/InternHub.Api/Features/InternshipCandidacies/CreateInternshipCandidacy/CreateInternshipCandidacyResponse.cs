using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public sealed record CreateInternshipCandidacyResponse(
    Guid Id,
    Guid StudentId,
    Guid OfferId,
    DateTime ApplicationDate,
    InternshipCandidacyStatus Status);
