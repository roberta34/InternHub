using InternHub.Api.Domain.InternshipCandidacies;
using InternHub.Api.Domain.Offers;
using InternHub.Api.Features.Shared;
using InternHub.Domain.Students;

namespace InternHub.Api.Features.InternshipCandidacies.CreateInternshipCandidacy;

public sealed class CreateInternshipCandidacyHandler(
    IInternshipCandidacyRepository candidacies,
    IStudentRepository students,
    IOfferRepository offers,
    TimeProvider timeProvider) : ICreateInternshipCandidacyHandler
{
    public Result<CreateInternshipCandidacyResponse> Handle(CreateInternshipCandidacyCommand command)
    {
        if (command.StudentId == Guid.Empty || command.OfferId == Guid.Empty)
            return Result<CreateInternshipCandidacyResponse>.Validation(
                "StudentId and OfferId must not be empty.");

        if (students.GetById(command.StudentId) is null)
            return Result<CreateInternshipCandidacyResponse>.NotFound(
                $"Student {command.StudentId} not found.");

        var offer = offers.GetById(command.OfferId);
        if (offer is null)
            return Result<CreateInternshipCandidacyResponse>.NotFound(
                $"Offer {command.OfferId} not found.");

        var applicationDate = timeProvider.GetUtcNow().UtcDateTime;

        if (offer.IsClosed)
            return Result<CreateInternshipCandidacyResponse>.Conflict(
                $"Offer {offer.Id} is closed.");

        if (offer.Deadline <= applicationDate)
            return Result<CreateInternshipCandidacyResponse>.Conflict(
                $"Offer {offer.Id} is past its application deadline.");

        if (candidacies.HasActiveForStudentAndOffer(command.StudentId, command.OfferId))
            return Result<CreateInternshipCandidacyResponse>.Conflict(
                $"Student {command.StudentId} already has an active candidacy for offer {command.OfferId}.");

        if (candidacies.CountActiveForStudent(command.StudentId) >= 5)
            return Result<CreateInternshipCandidacyResponse>.Conflict(
                $"Student {command.StudentId} already has the maximum of 5 active candidacies.");

        var candidacy = new InternshipCandidacy(
            Guid.NewGuid(),
            command.StudentId,
            command.OfferId,
            applicationDate,
            InternshipCandidacyStatus.Submitted);

        if (!candidacies.Add(candidacy))
            return Result<CreateInternshipCandidacyResponse>.Conflict(
                $"Candidacy {candidacy.Id} already exists.");

        return Result<CreateInternshipCandidacyResponse>.Success(
            new CreateInternshipCandidacyResponse(
                candidacy.Id,
                candidacy.StudentId,
                candidacy.OfferId,
                candidacy.ApplicationDate,
                candidacy.Status));
    }
}
