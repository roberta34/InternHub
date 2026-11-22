namespace InternHub.Api.Features.StudentCvFeedback;

public interface IStudentCvFeedbackService
{
    Task<StudentCvFeedbackResult> AnalyzeAsync(
        string cvContent,
        string offerRequirements,
        CancellationToken cancellationToken = default
    );
}