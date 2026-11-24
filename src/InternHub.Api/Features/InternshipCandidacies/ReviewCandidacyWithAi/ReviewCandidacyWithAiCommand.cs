using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.ReviewCandidacyWithAi;

public sealed record ReviewCandidacyWithAiCommand(
    Guid CandidacyId,
    string? CvText,
    ClaimsPrincipal? User = null);
