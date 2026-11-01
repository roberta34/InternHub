using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.GetInternshipCandidacyById;

public sealed record GetInternshipCandidacyByIdResponse(
    Guid Id,
    Guid StudentId,
    Guid OfferId,
    DateTime ApplicationDate,
    InternshipCandidacyStatus Status);
