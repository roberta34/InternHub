namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public sealed record GetStudentCandidacySummaryResponse(
    Guid StudentId,
    int TotalCount,
    int ActiveCount,
    int SubmittedCount,
    int ShortlistedCount,
    int AcceptedCount,
    int RejectedCount,
    int WaitlistedCount,
    int WithdrawnCount);
