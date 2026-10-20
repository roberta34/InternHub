namespace InternHub.Api.Features.Offers.GetOfferById;


public record GetOfferByIdResponse(
    Guid Id,
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    bool IsClosed,
    int Version);