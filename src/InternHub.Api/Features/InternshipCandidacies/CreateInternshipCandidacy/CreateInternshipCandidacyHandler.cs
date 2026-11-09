using InternHub.Api.Features.InternshipCandidacies.AtomicSubmission;
using InternHub.Api.Features.Shared;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public sealed class CreateInternshipCandidacyHandler(
    IAtomicCandidacySubmission submission) : ICreateInternshipCandidacyHandler
{
    public async Task<Result<CreateInternshipCandidacyResponse>> HandleAsync(
        CreateInternshipCandidacyCommand command,
        CancellationToken cancellationToken)
    {
        if (command.StudentId == Guid.Empty || command.OfferId == Guid.Empty)
            return Result<CreateInternshipCandidacyResponse>.Validation(
                "StudentId and OfferId must not be empty.");

        var result = await submission.SubmitAsync(
            command.StudentId,
            command.OfferId,
            cancellationToken);

        return result.Status switch
        {
            AtomicCandidacySubmissionStatus.Created when result.Candidacy is not null =>
                Result<CreateInternshipCandidacyResponse>.Success(
                    new CreateInternshipCandidacyResponse(
                        result.Candidacy.Id,
                        result.Candidacy.StudentId,
                        result.Candidacy.OfferId,
                        result.Candidacy.ApplicationDate,
                        result.Candidacy.Status)),
            AtomicCandidacySubmissionStatus.StudentNotFound =>
                Result<CreateInternshipCandidacyResponse>.NotFound(
                    $"Student {command.StudentId} not found."),
            AtomicCandidacySubmissionStatus.OfferNotFound =>
                Result<CreateInternshipCandidacyResponse>.NotFound(
                    $"Offer {command.OfferId} not found."),
            AtomicCandidacySubmissionStatus.OfferClosed =>
                Result<CreateInternshipCandidacyResponse>.Conflict(
                    $"Offer {command.OfferId} is closed."),
            AtomicCandidacySubmissionStatus.DeadlineExpired =>
                Result<CreateInternshipCandidacyResponse>.Conflict(
                    $"Offer {command.OfferId} is past its application deadline."),
            AtomicCandidacySubmissionStatus.DuplicateActive =>
                Result<CreateInternshipCandidacyResponse>.Conflict(
                    $"Student {command.StudentId} already has an active candidacy for offer {command.OfferId}."),
            AtomicCandidacySubmissionStatus.ActiveLimitReached =>
                Result<CreateInternshipCandidacyResponse>.Conflict(
                    $"Student {command.StudentId} already has the maximum of 5 active candidacies."),
            _ => throw new InvalidOperationException(
                $"Atomic submission returned an invalid result: {result.Status}.")
        };
    }
}
