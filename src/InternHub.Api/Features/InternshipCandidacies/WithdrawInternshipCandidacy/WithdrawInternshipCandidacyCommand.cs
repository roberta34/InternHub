using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.WithdrawInternshipCandidacy;

public sealed record WithdrawInternshipCandidacyCommand(
    Guid CandidacyId,
    ClaimsPrincipal? User = null);
