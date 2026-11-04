using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.GetOfferCandidacies;

public sealed record GetOfferCandidaciesResponse(
    Guid Id,
    Guid StudentId,
    string StudentFullName,
    DateTime ApplicationDate,
    InternshipCandidacyStatus Status);
