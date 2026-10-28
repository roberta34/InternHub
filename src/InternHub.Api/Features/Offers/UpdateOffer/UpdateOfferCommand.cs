namespace InternHub.Api.Features.Offers.UpdateOffer;

public sealed record UpdateOfferCommand(
    Guid Id,
    string Title,
    string CompanyName,
    string Requirements,
    DateTime Deadline,
    int MaximumAvailableSlots,
    int Version);
