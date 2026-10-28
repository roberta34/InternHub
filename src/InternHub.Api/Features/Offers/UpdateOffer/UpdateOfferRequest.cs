namespace InternHub.Api.Features.Offers.UpdateOffer;

public sealed record UpdateOfferRequest(
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    int Version);