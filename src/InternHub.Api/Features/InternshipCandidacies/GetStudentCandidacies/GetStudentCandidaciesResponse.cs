using InternHub.Api.Domain.InternshipCandidacies;

namespace InternHub.Api.Features.InternshipCandidacies.GetStudentCandidacies;

public sealed record GetStudentCandidaciesResponse(
    Guid Id,
    Guid OfferId,
    string OfferTitle,
    string CompanyName,
    DateTime ApplicationDate,
    InternshipCandidacyStatus Status);
