using System.Security.Claims;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public sealed record GetStudentCandidaciesQuery(
    Guid StudentId,
    ClaimsPrincipal? User = null);
