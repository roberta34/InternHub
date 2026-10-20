namespace InternHub.Api.Features.Offers.GetOffers;

public record GetOffersResponse(
    Guid Id,
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    bool IsClosed,
    int Version);