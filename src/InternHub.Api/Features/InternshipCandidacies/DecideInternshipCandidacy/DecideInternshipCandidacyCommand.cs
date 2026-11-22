using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.DecideInternshipCandidacy;

public sealed record DecideInternshipCandidacyCommand(
    Guid CandidacyId,
    string? Action,
    ClaimsPrincipal? User = null);
