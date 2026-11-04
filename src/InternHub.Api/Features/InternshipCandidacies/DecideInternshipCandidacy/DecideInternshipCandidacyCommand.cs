namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public sealed record DecideInternshipCandidacyCommand(Guid CandidacyId, string? Action);
