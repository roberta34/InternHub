using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public sealed record DecideInternshipCandidacyResponse(Guid Id, InternshipCandidacyStatus Status);
