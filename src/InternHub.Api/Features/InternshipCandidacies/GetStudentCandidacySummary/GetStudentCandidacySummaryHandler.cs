using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacySummary;

public sealed class GetStudentCandidacySummaryHandler(
    IStudentRepository students,
    IInternshipCandidacyRepository candidacies) : IGetStudentCandidacySummaryHandler
{
    public Result<GetStudentCandidacySummaryResponse> Handle(
        GetStudentCandidacySummaryQuery query)
    {
        if (students.GetById(query.StudentId) is null)
            return Result<GetStudentCandidacySummaryResponse>.NotFound(
                $"Student {query.StudentId} not found.");

        var studentCandidacies = candidacies.GetByStudent(query.StudentId);

        return Result<GetStudentCandidacySummaryResponse>.Success(
            new GetStudentCandidacySummaryResponse(
                query.StudentId,
                studentCandidacies.Count,
                studentCandidacies.Count(candidacy => candidacy.IsActive),
                studentCandidacies.Count(candidacy =>
                    candidacy.Status == InternshipCandidacyStatus.Submitted),
                studentCandidacies.Count(candidacy =>
                    candidacy.Status == InternshipCandidacyStatus.Shortlisted),
                studentCandidacies.Count(candidacy =>
                    candidacy.Status == InternshipCandidacyStatus.Accepted),
                studentCandidacies.Count(candidacy =>
                    candidacy.Status == InternshipCandidacyStatus.Rejected),
                studentCandidacies.Count(candidacy =>
                    candidacy.Status == InternshipCandidacyStatus.Waitlisted),
                studentCandidacies.Count(candidacy =>
                    candidacy.Status == InternshipCandidacyStatus.Withdrawn)));
    }
}
