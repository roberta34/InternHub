namespace InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;

public interface IAtomicCandidacySubmission
{
    Task<AtomicCandidacySubmissionResult> SubmitAsync(
        Guid studentId,
        Guid offerId,
        CancellationToken cancellationToken = default
    );
}