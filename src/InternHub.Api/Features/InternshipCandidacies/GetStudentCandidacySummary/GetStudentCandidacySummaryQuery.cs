using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public sealed record GetStudentCandidacySummaryQuery(
    Guid StudentId,
    ClaimsPrincipal? User = null);
