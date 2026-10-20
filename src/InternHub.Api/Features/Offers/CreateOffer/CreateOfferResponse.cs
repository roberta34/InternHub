namespace InternHub.Api.Features.Offers.CreateOffer;

public record CreateOfferResponse(
    Guid Id,
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    bool IsClosed,
    int Version);